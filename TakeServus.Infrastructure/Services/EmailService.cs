using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TakeServus.Application.Interfaces;
using TakeServus.Application.Settings;

public class EmailService : IEmailService
{
  private readonly SmtpSettings _settings;

  public EmailService(IOptions<SmtpSettings> settings)
  {
    _settings = settings.Value;
  }

  public async Task SendEmailAsync(string to, string subject, string body)
  {
    var message = new MailMessage
    {
      From = new MailAddress(_settings.FromEmail, _settings.FromName),
      Subject = subject,
      Body = body,
      IsBodyHtml = true
    };

    message.To.Add(to);

    using var client = new SmtpClient(_settings.Host, _settings.Port)
    {
      Credentials = new NetworkCredential(_settings.Username, _settings.Password),
      EnableSsl = _settings.EnableSsl
    };

    await client.SendMailAsync(message);
  }
}