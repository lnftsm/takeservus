using Microsoft.Extensions.DependencyInjection;
using TakeServus.Application.Mappings;

namespace TakeServus.Application;
public static class ApplicationServiceCollectionExtensions
{
  public static IServiceCollection AddApplicationServices(this IServiceCollection services)
  {
    services.AddAutoMapper(typeof(MappingProfile));

    return services;
  }
}
