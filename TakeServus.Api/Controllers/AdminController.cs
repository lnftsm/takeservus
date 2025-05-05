using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Dispatcher")]
public class AdminController : ControllerBase
{
  private readonly TakeServusDbContext _context;

  public AdminController(TakeServusDbContext context)
  {
    _context = context;
  }

  [HttpGet("technicians/{technicianId}/ratings")]
  public async Task<IActionResult> GetTechnicianRatings(Guid technicianId)
  {
    var technician = await _context.Technicians
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.Id == technicianId);

    if (technician == null) return NotFound("Technician not found");

    var jobIds = await _context.Jobs
        .Where(j => j.TechnicianId == technicianId)
        .Select(j => j.Id)
        .ToListAsync();

    var feedbacks = await _context.JobFeedbacks
        .Where(f => jobIds.Contains(f.JobId))
        .ToListAsync();

    if (!feedbacks.Any())
    {
      return Ok(new
      {
        technicianId,
        technicianName = technician.User.FullName,
        averageRating = 0,
        totalFeedbacks = 0,
        satisfactionRate = 0
      });
    }

    var averageRating = feedbacks.Where(f => f.Rating.HasValue).Average(f => f.Rating!.Value);
    var satisfactionRate = 100.0 * feedbacks.Count(f => f.IsSatisfied) / feedbacks.Count;

    return Ok(new
    {
      technicianId,
      technicianName = technician.User.FullName,
      averageRating = Math.Round(averageRating, 2),
      totalFeedbacks = feedbacks.Count,
      satisfactionRate = Math.Round(satisfactionRate, 1)
    });
  }
}