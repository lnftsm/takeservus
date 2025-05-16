namespace TakeServus.Application.DTOs.Materials;

public class CreateMaterialRequest
{
  public string Name { get; set; } = default!;
  public string Unit { get; set; } = default!;
  public decimal UnitPrice { get; set; }
  public int StockQuantity { get; set; }
}