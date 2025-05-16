namespace TakeServus.Application.DTOs.Technicians;
public class TechnicianDashboardItemResponse
{
  public Guid Id { get; set; }
  public string FullName { get; set; } = default!;
  public int AssignedJobs { get; set; }
  public int CompletedJobs { get; set; }
  public bool IsAvailable { get; set; }
}