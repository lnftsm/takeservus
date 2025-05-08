using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TakeServus.Application.Interfaces;
using TakeServus.Application.Settings;
using TakeServus.Infrastructure.Services;

namespace TakeServus.Infrastructure.Extensions
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection UseFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
      services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));
      services.Configure<FirebaseSettings>(configuration.GetSection("Firebase"));

      var fileStorageSettings = configuration.GetSection("FileStorage").Get<FileStorageSettings>();

      if (fileStorageSettings?.UseFirebase == true)
      {
        services.AddSingleton<IFileStorageService, FirebaseStorageService>();
      }
      else
      {
        services.AddSingleton<IFileStorageService, LocalStorageService>();
      }

      return services;
    }
  }
}