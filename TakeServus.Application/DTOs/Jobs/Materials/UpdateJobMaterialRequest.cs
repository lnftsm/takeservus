namespace TakeServus.Application.DTOs.Jobs.Materials;

public class UpdateJobMaterialRequest
{
    public Guid JobId { get; set; }
    public Guid MaterialId { get; set; }
    public int QuantityUsed { get; set; }
    public decimal UnitPrice { get; set; }
}