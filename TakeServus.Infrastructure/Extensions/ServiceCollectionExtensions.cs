using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TakeServus.Application.Interfaces;
using TakeServus.Infrastructure.Services;
using TakeServus.Shared.Settings;

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
        services.AddSingleton<IFirebaseStorageService, FirebaseStorageService>();
      }
      else
      {
        services.AddSingleton<IFileStorageService, FileStorageService>();
      }

      return services;
    }
  }
}