using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TakeServus.Application.DTOs.Common;
using TakeServus.Application.DTOs.Feedback;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
  private readonly TakeServusDbContext _context;

  public FeedbackController(TakeServusDbContext context)
  {
    _context = context;
  }

  [HttpPost]
  [Authorize(Roles = "Customer")]
  public async Task<IActionResult> SubmitFeedback([FromBody] CreateJobFeedbackRequest request)
  {
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == Guid.Parse(userId));
    if (customer == null) return NotFound("Customer not found");

    var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobId);
    if (job == null || job.CustomerId != customer.Id)
      return BadRequest("Invalid job for this customer");

    var exists = await _context.JobFeedbacks.AnyAsync(f =>
        f.JobId == request.JobId && f.CustomerId == customer.Id);
    if (exists)
      return BadRequest("Feedback already submitted for this job");

    var feedback = new JobFeedback
    {
      Id = Guid.NewGuid(),
      JobId = request.JobId,
      CustomerId = customer.Id,
      IsSatisfied = request.IsSatisfied,
      Rating = request.Rating,
      Comment = request.Comment,
      SubmittedAt = DateTime.UtcNow
    };

    _context.JobFeedbacks.Add(feedback);
    await _context.SaveChangesAsync();

    return Ok("Feedback submitted successfully");
  }

  [HttpGet("{jobId}")]
  [Authorize(Roles = "Customer")]
  public async Task<IActionResult> GetMyFeedback(Guid jobId)
  {
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == Guid.Parse(userId));
    if (customer == null) return NotFound("Customer not found");

    var feedback = await _context.JobFeedbacks
        .FirstOrDefaultAsync(f => f.JobId == jobId && f.CustomerId == customer.Id);

    if (feedback == null) return NotFound("Feedback not found for this job");

    return Ok(new JobFeedbackResponse
    {
      JobId = feedback.JobId,
      Rating = feedback.Rating ?? 0,
      Comment = feedback.Comment,
      SubmittedAt = feedback.SubmittedAt
    });
  }

  [HttpGet("job/{jobId}")]
  [Authorize(Roles = "Owner,Dispatcher,Technician")]
  public async Task<ActionResult<JobFeedbackResponse>> GetFeedbackByJob(Guid jobId)
  {
    var feedback = await _context.JobFeedbacks
        .Include(f => f.Job)
        .ThenInclude(j => j.Customer)
        .FirstOrDefaultAsync(f => f.JobId == jobId);

    if (feedback == null) return NotFound();

    return Ok(new JobFeedbackResponse
    {
      JobId = feedback.JobId,
      Rating = feedback.Rating ?? 0,
      Comment = feedback.Comment,
      SubmittedAt = feedback.SubmittedAt
    });
  }

  [HttpGet("list")]
  [Authorize(Roles = "Owner,Dispatcher")]
  public async Task<ActionResult<PagedResult<JobFeedbackResponse>>> GetFeedbackList(
      [FromQuery] string? technicianName,
      [FromQuery] string? jobTitle,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10)
  {
    var query = _context.JobFeedbacks
        .Include(f => f.Job)
            .ThenInclude(j => j.Technician)
                .ThenInclude(t => t.User)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(technicianName))
    {
      query = query.Where(f => f.Job.Technician.User.FullName.Contains(technicianName));
    }

    if (!string.IsNullOrWhiteSpace(jobTitle))
    {
      query = query.Where(f => f.Job.Title.Contains(jobTitle));
    }

    var totalCount = await query.CountAsync();

    var items = await query
        .OrderByDescending(f => f.SubmittedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(f => new JobFeedbackResponse
        {
          JobId = f.JobId,
          Rating = f.Rating ?? 0,
          Comment = f.Comment,
          SubmittedAt = f.SubmittedAt
        }).ToListAsync();

    return Ok(new PagedResult<JobFeedbackResponse>
    {
      Items = items,
      TotalCount = totalCount,
      Page = page,
      PageSize = pageSize
    });
  }
}