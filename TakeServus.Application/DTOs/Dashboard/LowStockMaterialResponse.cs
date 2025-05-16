namespace TakeServus.Application.DTOs.Dashboard;
public class LowStockMaterialResponse
{
    public string MaterialName { get; set; } = default!;
    public int StockQuantity { get; set; }
}