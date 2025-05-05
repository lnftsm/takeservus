namespace TakeServus.Application.DTOs.Jobs;

public class JobMaterialResponse
{
    public string MaterialName { get; set; } = default!;
    public int QuantityUsed { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total => QuantityUsed * UnitPrice;
}