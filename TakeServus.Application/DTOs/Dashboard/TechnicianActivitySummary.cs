namespace TakeServus.Application.DTOs.Dashboard;

public class TechnicianActivitySummary
{
  public string TechnicianName { get; set; } = default!;
  public int AssignedJobs { get; set; }
  public int CompletedJobs { get; set; }
  public double AverageRating { get; set; }
}