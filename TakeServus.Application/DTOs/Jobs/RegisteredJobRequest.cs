namespace TakeServus.Application.DTOs.Jobs;

public class RegisteredJobRequest
{
  public Guid CustomerId { get; set; }
  public string Title { get; set; } = default!;
  public string? Description { get; set; }
  public DateTime ScheduledAt { get; set; }
}