namespace TakeServus.Domain.Entities;

public class JobMaterial
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid MaterialId { get; set; }
    public int QuantityUsed { get; set; }

    public Job Job { get; set; } = default!;
    public Material Material { get; set; } = default!;
}