namespace TakeServus.Application.DTOs.Jobs;

public class AssignMaterialRequest
{
    public Guid JobId { get; set; }
    public Guid MaterialId { get; set; }
    public int QuantityUsed { get; set; }
}