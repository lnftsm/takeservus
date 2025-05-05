using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TakeServus.Application.DTOs.Jobs;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class FeedbackController : ControllerBase
{
  private readonly TakeServusDbContext _context;

  public FeedbackController(TakeServusDbContext context)
  {
    _context = context;
  }

  [HttpPost]
  public async Task<IActionResult> SubmitFeedback([FromBody] CreateJobFeedbackRequest request)
  {
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == Guid.Parse(userId));
    if (customer == null) return NotFound("Customer not found");

    var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobId);
    if (job == null || job.CustomerId != customer.Id) return BadRequest("Invalid job for this customer");

    var exists = await _context.JobFeedbacks.AnyAsync(f => f.JobId == request.JobId && f.CustomerId == customer.Id);
    if (exists) return BadRequest("Feedback already submitted for this job");

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
  public async Task<IActionResult> GetFeedback(Guid jobId)
  {
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userId == null) return Unauthorized();

    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == Guid.Parse(userId));
    if (customer == null) return NotFound("Customer not found");

    var feedback = await _context.JobFeedbacks
        .FirstOrDefaultAsync(f => f.JobId == jobId && f.CustomerId == customer.Id);

    if (feedback == null) return NotFound("Feedback not found for this job");

    return Ok(new
    {
      feedback.JobId,
      feedback.IsSatisfied,
      feedback.Rating,
      feedback.Comment,
      feedback.SubmittedAt
    });
  }
}