using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Materials;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Dispatcher")]
public class MaterialController : ControllerBase
{
  private readonly TakeServusDbContext _context;

  public MaterialController(TakeServusDbContext context)
  {
    _context = context;
  }

  [HttpGet("list")]
  [AllowAnonymous] // Optional: allow public access
  public async Task<ActionResult<object>> GetAll(
      [FromQuery] string? keyword,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10)
  {
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;

    var query = _context.Materials
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(keyword))
      query = query.Where(m => m.Name.ToLower().Contains(keyword.ToLower()));

    var total = await query.CountAsync();

    var items = await query
        .OrderBy(m => m.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(m => new MaterialResponse
        {
          Id = m.Id,
          Name = m.Name,
          Unit = m.Unit,
          UnitPrice = m.UnitPrice,
          StockQuantity = m.StockQuantity,
        }).ToListAsync();

    return Ok(new
    {
      Items = items,
      TotalCount = total,
      Page = page,
      PageSize = pageSize
    });
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateMaterialRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
      return BadRequest("Material name is required.");

    var material = new Material
    {
      Id = Guid.NewGuid(),
      Name = request.Name,
      Unit = request.Unit,
      UnitPrice = request.UnitPrice,
      StockQuantity = request.StockQuantity,
      IsActive = true,
      CreatedAt = DateTime.UtcNow
    };

    await _context.Materials.AddAsync(material);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetAll), new { id = material.Id }, material.Id);
  }

  [HttpPut]
  public async Task<IActionResult> Update([FromBody] UpdateMaterialRequest request)
  {
    var material = await _context.Materials.FindAsync(request.Id);
    if (material == null || !material.IsActive) return NotFound("Material not found or inactive.");

    material.Name = request.Name;
    material.Unit = request.Unit;
    material.UnitPrice = request.UnitPrice;
    material.StockQuantity = request.StockQuantity;
    material.IsActive = request.IsActive;

    await _context.SaveChangesAsync();
    return Ok(new { message = "Material updated successfully." });
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    var material = await _context.Materials.FindAsync(id);
    if (material == null || !material.IsActive) return NotFound("Material not found or already inactive.");

    material.IsActive = false;
    await _context.SaveChangesAsync();

    return Ok(new { message = "Material archived (soft-deleted)." });
  }

  [HttpPost("seed")]
  [AllowAnonymous] // Optional: public access for testing/dev
  public async Task<IActionResult> Seed()
  {
    var materials = new List<Material>
        {
            new() { Id = Guid.NewGuid(), Name = "PVC Pipe", Unit = "pcs", UnitPrice = 12.50m, StockQuantity = 100, CreatedAt = DateTime.UtcNow, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Wire Connector", Unit = "pcs", UnitPrice = 1.25m, StockQuantity = 500, CreatedAt = DateTime.UtcNow, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Copper Coil", Unit = "m", UnitPrice = 7.80m, StockQuantity = 80, CreatedAt = DateTime.UtcNow, IsActive = true }
        };

    await _context.Materials.AddRangeAsync(materials);
    await _context.SaveChangesAsync();

    return Ok("Seed completed.");
  }
}