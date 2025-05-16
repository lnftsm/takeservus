using TakeServus.Domain.Common;

namespace TakeServus.Domain.Entities;
public class JobNote : BaseEntityWithAudit
{
    public Guid JobId { get; set; }
    public string Note { get; set; } = default!;

    public Job Job { get; set; } = default!;
    public User CreatedByUser { get; set; } = default!;
}