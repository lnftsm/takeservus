using TakeServus.Domain.Common;

namespace TakeServus.Domain.Entities;

public class JobPhoto : BaseEntityWithAudit
{
    public Guid JobId { get; set; }
    public string PhotoUrl { get; set; } = default!;
    public DateTime UploadedAt { get; set; }

    public Job Job { get; set; } = default!;
}