using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TakeServus.Application.Interfaces;

namespace TakeServus.Infrastructure.Background;

public class QueuedEmailWorker : BackgroundService
{
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<QueuedEmailWorker> _logger;

  public QueuedEmailWorker(IServiceProvider serviceProvider, ILogger<QueuedEmailWorker> logger)
  {
    _serviceProvider = serviceProvider;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("QueuedEmailWorker started");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IQueuedEmailService>();
        await emailService.ProcessPendingEmailsAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred while processing queued emails");
      }

      await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
    }

    _logger.LogInformation("QueuedEmailWorker stopped");
  }
}