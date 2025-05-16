using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TakeServus.Application.Interfaces;
using TakeServus.Infrastructure.Services;
using TakeServus.Shared.Settings;

namespace TakeServus.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
  public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
  {
    // Register strongly typed settings
    services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
    services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
    services.Configure<FirebaseSettings>(configuration.GetSection("FirebaseSettings"));
    services.Configure<FileStorageSettings>(configuration.GetSection("FileStorageSettings"));

    // Add HttpContextAccessor for audit handling
    services.AddHttpContextAccessor();

    // Register core services
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IQueuedEmailService, QueuedEmailService>();
    services.AddScoped<IFileStorageService, FileStorageService>();
    services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();
    services.AddScoped<IInvoiceService, InvoiceService>();

    return services;
  }
}