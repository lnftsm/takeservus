namespace TakeServus.Application.DTOs.Technicians;

public class TechnicianRatingSummaryResponse
{
  public Guid TechnicianId { get; set; }
  public string TechnicianName { get; set; } = default!;
  public double AverageRating { get; set; }
  public int TotalFeedbacks { get; set; }
  public double SatisfactionRate { get; set; } // 0-100%
}