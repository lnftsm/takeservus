namespace TakeServus.Domain.Entities;

public class QueuedEmail
{
  public Guid Id { get; set; }
  public string To { get; set; } = default!;
  public string Subject { get; set; } = default!;
  public string Body { get; set; } = default!;
  public DateTime CreatedAt { get; set; }
  public bool IsSent { get; set; } = false;
  public DateTime? SentAt { get; set; }
  public int RetryCount { get; set; } = 0;
}