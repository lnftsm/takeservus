namespace TakeServus.Application.DTOs.Jobs;

public class UnassignedJobResponse
{
  public Guid Id { get; set; }
  public string Title { get; set; } = default!;
  public DateTime? RequestedAt { get; set; }
  public string? Description { get; set; }
  public string? CustomerName { get; set; }
}