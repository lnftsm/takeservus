using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TakeServus.Application.Interfaces;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Infrastructure.Services;

public class QueuedEmailService : IQueuedEmailService
{
  private readonly TakeServusDbContext _context;
  private readonly IEmailService _emailService;
  private readonly ILogger<QueuedEmailService> _logger;

  public QueuedEmailService(TakeServusDbContext context, IEmailService emailService, ILogger<QueuedEmailService> logger)
  {
    _context = context;
    _emailService = emailService;
    _logger = logger;
  }

  public async Task EnqueueEmailAsync(string to, string subject, string body)
  {
    var email = new QueuedEmail
    {
      Id = Guid.NewGuid(),
      To = to,
      Subject = subject,
      Body = body,
      CreatedAt = DateTime.UtcNow
    };

    _context.QueuedEmails.Add(email);
    await _context.SaveChangesAsync();
  }

  public async Task ProcessPendingEmailsAsync()
  {
    var pendingEmails = await _context.QueuedEmails
        .Where(e => !e.IsSent && e.RetryCount < 3)
        .OrderBy(e => e.CreatedAt)
        .Take(10)
        .ToListAsync();

    foreach (var email in pendingEmails)
    {
      try
      {
        await _emailService.SendEmailAsync(email.To, email.Subject, email.Body);
        email.IsSent = true;
        email.SentAt = DateTime.UtcNow;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to send queued email to {Email}", email.To);
        email.RetryCount++;
      }
    }

    await _context.SaveChangesAsync();
  }
}
