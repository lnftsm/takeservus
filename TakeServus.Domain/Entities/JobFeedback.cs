using TakeServus.Domain.Common;

namespace TakeServus.Domain.Entities;

public class JobFeedback : BaseEntityWithAudit
{
  public Guid JobId { get; set; }
  public Guid CustomerId { get; set; }

  public bool IsSatisfied { get; set; }
  public int? Rating { get; set; } // 1 to 5
  public string? Comment { get; set; }

  public DateTime SubmittedAt { get; set; }

  public Job Job { get; set; } = default!;
  public Customer Customer { get; set; } = default!;
}