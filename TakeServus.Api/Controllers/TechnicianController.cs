using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Technician")]
public class TechnicianController : ControllerBase
{
  private readonly TakeServusDbContext _context;

  public TechnicianController(TakeServusDbContext context)
  {
    _context = context;
  }

  [HttpGet("my-feedback")]
  public async Task<IActionResult> GetMyFeedbacks()
  {
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var technician = await _context.Technicians
        .FirstOrDefaultAsync(t => t.UserId == Guid.Parse(userId));
    if (technician == null) return NotFound("Technician not found");

    var feedbacks = await _context.Jobs
        .Where(j => j.TechnicianId == technician.Id)
        .Join(_context.JobFeedbacks,
              job => job.Id,
              feedback => feedback.JobId,
              (job, feedback) => new
              {
                JobTitle = job.Title,
                IsSatisfied = feedback.IsSatisfied,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                SubmittedAt = feedback.SubmittedAt
              })
        .OrderByDescending(f => f.SubmittedAt)
        .ToListAsync();

    return Ok(feedbacks);
  }
}