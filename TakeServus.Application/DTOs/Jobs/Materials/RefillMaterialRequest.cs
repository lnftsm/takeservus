namespace TakeServus.Application.DTOs.Jobs.Materials;

public class RefillMaterialRequest
{
    public Guid MaterialId { get; set; }
    public int QuantityToAdd { get; set; }
}