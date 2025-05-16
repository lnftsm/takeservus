using TakeServus.Domain.Common;

namespace TakeServus.Domain.Entities;

public class JobMaterial : BaseEntityWithAudit
{
    public Guid JobId { get; set; }
    public Guid MaterialId { get; set; }

    public decimal UnitPrice { get; set; }
    public int QuantityUsed { get; set; }

    public Job Job { get; set; } = default!;
    public Material Material { get; set; } = default!;
}