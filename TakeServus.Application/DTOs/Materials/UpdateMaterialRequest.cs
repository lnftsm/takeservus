namespace TakeServus.Application.DTOs.Materials;
public class UpdateMaterialRequest
{
  public Guid Id { get; set; }
  public string Name { get; set; } = default!;
  public string Unit { get; set; } = default!;
  public decimal UnitPrice { get; set; }
  public int StockQuantity { get; set; }
  public bool IsActive { get; set; } = true;
}