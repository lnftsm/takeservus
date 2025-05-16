using TakeServus.Domain.Common;

namespace TakeServus.Domain.Entities;
public class Material : BaseEntityWithAudit
{
    public string Name { get; set; } = default!;
    public string Unit { get; set; } = default!; // e.g., pcs, liters
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public ICollection<JobMaterial> JobMaterials { get; set; } = new List<JobMaterial>();
}