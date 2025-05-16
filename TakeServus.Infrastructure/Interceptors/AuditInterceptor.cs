using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TakeServus.Domain.Common;

namespace TakeServus.Infrastructure.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
      DbContextEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default)
  {
    var context = eventData.Context;
    if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

    var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

    foreach (var entry in context.ChangeTracker.Entries<BaseEntityWithAudit>())
    {
      switch (entry.State)
      {
        case EntityState.Added:
          entry.Entity.CreatedAt = DateTime.UtcNow;
          entry.Entity.CreatedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty;
          entry.Entity.CreatedByUserFullName = userName;
          entry.Entity.IsDeleted = false;
          entry.Entity.IsActive = true;
          break;
        case EntityState.Modified:
          entry.Entity.ModifiedAt = DateTime.UtcNow;
          entry.Entity.ModifiedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty;
          entry.Entity.ModifiedByUserFullName = userName;
          break;
      }
    }

    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }
}