namespace TakeServus.Application.DTOs.Jobs.Feedback;

public class CreateJobFeedbackRequest
{
  public Guid JobId { get; set; }
  public bool IsSatisfied { get; set; }
  public int? Rating { get; set; } // 1-5
  public string? Comment { get; set; }
}