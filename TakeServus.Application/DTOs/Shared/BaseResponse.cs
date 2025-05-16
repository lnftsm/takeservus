namespace TakeServus.Application.DTOs.Shared;

public abstract class BaseResponse
{
  public Guid Id { get; set; }
  public AuditInfo Audit { get; set; } = default!;
}

public class AuditInfo
{
  public string CreatedBy { get; set; } = default!;
  public DateTime CreatedAt { get; set; }
  public string? ModifiedBy { get; set; }
  public DateTime? ModifiedAt { get; set; }
}