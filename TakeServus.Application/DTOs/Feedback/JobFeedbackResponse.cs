using System;

namespace TakeServus.Application.DTOs.Feedback;

public class JobFeedbackResponse
{
  public Guid JobId { get; set; }
  public int Rating { get; set; }
  public string? Comment { get; set; }
  public DateTime SubmittedAt { get; set; }
}