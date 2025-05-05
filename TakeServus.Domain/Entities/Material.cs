namespace TakeServus.Domain.Entities;

public class Material
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }

    public ICollection<JobMaterial> JobMaterials { get; set; } = new List<JobMaterial>();
}