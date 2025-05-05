using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Materials;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Dispatcher")]
public class MaterialsController : ControllerBase
{
    private readonly TakeServusDbContext _context;

    public MaterialsController(TakeServusDbContext context)
    {
        _context = context;
    }

    [HttpPost("refill")]
    public async Task<IActionResult> Refill([FromBody] RefillMaterialRequest request)
    {
        var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == request.MaterialId);
        if (material == null)
            return NotFound("Material not found.");

        material.StockQuantity += request.QuantityToAdd;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            material.Id,
            material.Name,
            material.StockQuantity
        });
    }
}