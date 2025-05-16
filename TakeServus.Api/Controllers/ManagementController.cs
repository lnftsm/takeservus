using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Jobs;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Dispatcher")]
public class ManagementController : ControllerBase
{
  private readonly TakeServusDbContext _context;

  public ManagementController(TakeServusDbContext context)
  {
    _context = context;
  }



  [HttpGet("unassigned")]
  [Authorize(Roles = "Owner,Dispatcher")]
  public async Task<IActionResult> GetUnassignedJobs()
  {
    var jobs = await _context.Jobs
        .Where(j => !j.IsDeleted && !j.IsAssigned)
        .Include(j => j.Customer)
        .Select(j => new UnassignedJobResponse
        {
          Id = j.Id,
          Title = j.Title,
          Description = j.Description,
          RequestedAt = j.CreatedAt,
          CustomerName = j.Customer.FullName
        }).ToListAsync();

    return Ok(jobs);
  }
}