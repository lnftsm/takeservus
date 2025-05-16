using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Persistence;

public static class PersistenceServiceCollectionExtensions
{
  public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<TakeServusDbContext>(options =>
    {
      var connStr = configuration.GetConnectionString("DefaultConnection");
      options.UseNpgsql(connStr);
    });

    return services;
  }
}