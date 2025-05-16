using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TakeServus.Application.DTOs.Technicians;
using TakeServus.Application.DTOs.Users;
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

    return Ok(new { items = feedbacks });
  }

  [HttpGet("{technicianId}/ratings")]
  [AllowAnonymous]
  public async Task<ActionResult<TechnicianRatingSummaryResponse>> GetTechnicianRatings(Guid technicianId)
  {
    var technician = await _context.Technicians
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.Id == technicianId);

    if (technician == null)
      return NotFound("Technician not found.");

    var jobIds = await _context.Jobs
        .Where(j => j.TechnicianId == technicianId)
        .Select(j => j.Id)
        .ToListAsync();

    var feedbacks = await _context.JobFeedbacks
        .Where(f => jobIds.Contains(f.JobId))
        .ToListAsync();

    if (!feedbacks.Any())
    {
      return Ok(new TechnicianRatingSummaryResponse
      {
        TechnicianId = technicianId,
        TechnicianName = technician.User.FullName,
        AverageRating = 0,
        TotalFeedbacks = 0,
        SatisfactionRate = 0
      });
    }

    var averageRating = feedbacks.Where(f => f.Rating.HasValue).Average(f => f.Rating!.Value);
    var satisfactionRate = 100.0 * feedbacks.Count(f => f.IsSatisfied) / feedbacks.Count;

    return Ok(new TechnicianRatingSummaryResponse
    {
      TechnicianId = technicianId,
      TechnicianName = technician.User.FullName,
      AverageRating = Math.Round(averageRating, 2),
      TotalFeedbacks = feedbacks.Count,
      SatisfactionRate = Math.Round(satisfactionRate, 1)
    });
  }

  [HttpPost("location")]
  public async Task<IActionResult> UpdateLocation([FromBody] TechnicianLocationUpdateRequest request)
  {
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var technician = await _context.Technicians
        .FirstOrDefaultAsync(t => t.UserId == Guid.Parse(userId));
    if (technician == null) return NotFound("Technician not found");

    technician.CurrentLatitude = request.Latitude;
    technician.CurrentLongitude = request.Longitude;

    await _context.SaveChangesAsync();
    return Ok(new { Message = "Location updated successfully" });
  }

  [HttpGet("job-performance")]
  public async Task<ActionResult<List<TechnicianJobPerformanceResponse>>> GetJobPerformance()
  {
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var technician = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == Guid.Parse(userId));
    if (technician == null) return NotFound("Technician not found");

    var endDate = DateTime.Today;
    var startDate = endDate.AddDays(-6);

    var jobData = await _context.Jobs
        .Where(j => j.TechnicianId == technician.Id &&
                    j.CompletedAt.HasValue &&
                    j.CompletedAt.Value.Date >= startDate)
        .ToListAsync();

    var performance = Enumerable.Range(0, 7)
        .Select(i => startDate.AddDays(i))
        .Select(date => new TechnicianJobPerformanceResponse
        {
          Date = date,
          JobsCompleted = jobData.Count(j => j.CompletedAt!.Value.Date == date)
        })
        .ToList();

    return Ok(performance);
  }
}