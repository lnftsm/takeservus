using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TakeServus.Application.DTOs.Jobs.Materials;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/job-materials")]
[Authorize]
public class JobMaterialsController : ControllerBase
{
  private readonly TakeServusDbContext _context;

  public JobMaterialsController(TakeServusDbContext context)
  {
    _context = context;
  }

  [HttpPost]
  [Authorize(Roles = "Technician")]
  public async Task<IActionResult> AddMaterial(CreateJobMaterialRequest request)
  {
    if (request.JobId == Guid.Empty || request.MaterialId == Guid.Empty)
      return BadRequest("Job ID and Material ID are required.");
    if (request.QuantityUsed <= 0 || request.QuantityUsed > 1000)
      return BadRequest("Quantity must be between 1 and 1000.");

    var job = await _context.Jobs.FindAsync(request.JobId);
    var material = await _context.Materials.FindAsync(request.MaterialId);

    if (job == null || job.IsDeleted) return NotFound("Job not found or archived.");
    if (material == null) return NotFound("Material not found.");
    if (job.Status != "Started" && job.Status != "Completed")
      return BadRequest("Job must be in 'Started' or 'Completed' status.");
    if (material.StockQuantity < request.QuantityUsed)
      return BadRequest("Insufficient stock.");

    material.StockQuantity -= request.QuantityUsed;

    var jobMaterial = new JobMaterial
    {
      Id = Guid.NewGuid(),
      JobId = request.JobId,
      MaterialId = request.MaterialId,
      QuantityUsed = request.QuantityUsed,
      UnitPrice = material.UnitPrice
    };

    _context.JobMaterials.Add(jobMaterial);

    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    _context.JobActivities.Add(new JobActivity
    {
      Id = Guid.NewGuid(),
      JobId = request.JobId,
      ActivityType = "MaterialAssigned",
      Details = $"Assigned {request.QuantityUsed} x {material.Name}",
      PerformedAt = DateTime.UtcNow,
      PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
    });

    await _context.SaveChangesAsync();
    return Ok(new JobMaterialResponse
    {
      MaterialName = material.Name,
      QuantityUsed = jobMaterial.QuantityUsed,
      Unit = material.Unit,
      UnitPrice = material.UnitPrice
    });
  }

  [HttpPut("{id}")]
  [Authorize(Roles = "Technician")]
  public async Task<IActionResult> UpdateMaterial(Guid id, UpdateJobMaterialRequest request)
  {
    var jobMaterial = await _context.JobMaterials
        .Include(jm => jm.Material)
        .FirstOrDefaultAsync(jm => jm.Id == id);

    if (jobMaterial == null) return NotFound("Job material not found.");

    var delta = request.QuantityUsed - jobMaterial.QuantityUsed;
    if (delta > 0 && jobMaterial.Material.StockQuantity < delta)
      return BadRequest("Insufficient stock to increase quantity.");

    jobMaterial.Material.StockQuantity -= delta;
    jobMaterial.QuantityUsed = request.QuantityUsed;
    jobMaterial.UnitPrice = request.UnitPrice;

    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    _context.JobActivities.Add(new JobActivity
    {
      Id = Guid.NewGuid(),
      JobId = jobMaterial.JobId,
      ActivityType = "MaterialUpdated",
      Details = $"Updated {jobMaterial.Material.Name} to qty {request.QuantityUsed}",
      PerformedAt = DateTime.UtcNow,
      PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
    });

    await _context.SaveChangesAsync();
    return Ok(new JobMaterialResponse
    {
      MaterialName = jobMaterial.Material.Name,
      QuantityUsed = jobMaterial.QuantityUsed,
      Unit = jobMaterial.Material.Unit,
      UnitPrice = jobMaterial.UnitPrice
    });
  }

  [HttpDelete("{jobId}/material/{materialId}")]
  [Authorize]
  public async Task<IActionResult> DeleteMaterial(Guid jobId, Guid materialId)
  {
    var material = await _context.JobMaterials
        .Include(m => m.Material)
        .FirstOrDefaultAsync(m => m.Id == materialId && m.JobId == jobId);

    if (material == null) return NotFound("Material not found");

    _context.JobMaterials.Remove(material);

    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    _context.JobActivities.Add(new JobActivity
    {
      Id = Guid.NewGuid(),
      JobId = jobId,
      ActivityType = "MaterialRemoved",
      Details = $"Material '{material.Material.Name}' removed by {User.FindFirst(ClaimTypes.Name)?.Value}",
      PerformedAt = DateTime.UtcNow,
      PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
    });

    await _context.SaveChangesAsync();
    return Ok(new { Message = "Material removed successfully." });
  }
}