namespace TakeServus.Application.DTOs.Jobs.Materials;

public class JobMaterialResponse
{
    public string MaterialName { get; set; } = default!;
    public int QuantityUsed { get; set; }
    public string Unit { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public decimal Total => QuantityUsed * UnitPrice;
}