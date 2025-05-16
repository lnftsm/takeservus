namespace TakeServus.Application.DTOs.Jobs.Feedback;

public class JobFeedbackResponse
{
  public Guid Id { get; set; }
  public string JobTitle { get; set; } = default!;

  public bool IsSatisfied { get; set; }
  public int? Rating { get; set; }
  public string? Comment { get; set; }
  public DateTime SubmittedAt { get; set; }
}