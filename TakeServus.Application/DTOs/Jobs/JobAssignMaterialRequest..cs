namespace TakeServus.Application.DTOs.Jobs;

public class JobAssignMaterialRequest
{
    public Guid JobId { get; set; }
    public Guid MaterialId { get; set; }
    public int QuantityUsed { get; set; }
}