using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Dashboard;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Dispatcher")]
public class DashboardController : ControllerBase
{
    private readonly TakeServusDbContext _context;

    public DashboardController(TakeServusDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary()
    {
        var jobs = _context.Jobs.AsQueryable();
        var users = _context.Users.AsQueryable();
        var technicians = _context.Technicians.AsQueryable();
        var materials = _context.Materials.AsQueryable();
        var customers = _context.Customers.AsQueryable();

        var result = new DashboardSummaryResponse
        {
            TotalJobs = await jobs.CountAsync(),
            ScheduledJobs = await jobs.CountAsync(j => j.Status == "Scheduled"),
            EnRouteJobs = await jobs.CountAsync(j => j.Status == "EnRoute"),
            StartedJobs = await jobs.CountAsync(j => j.Status == "Started"),
            CompletedJobs = await jobs.CountAsync(j => j.Status == "Completed"),

            ActiveTechnicians = await users.CountAsync(u => u.Role == "Technician" && u.IsActive),
            TotalCustomers = await customers.CountAsync(),

            LowStockMaterials = await materials
                .Where(m => m.StockQuantity < 10)
                .Select(m => new LowStockMaterialDto
                {
                    Name = m.Name,
                    StockQuantity = m.StockQuantity
                }).ToListAsync()
        };

        return Ok(result);
    }

    [HttpGet("job-summary")]
    public async Task<IActionResult> GetJobSummary()
    {
        var result = await _context.Jobs
            .GroupBy(j => j.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("revenue-summary")]
    public async Task<IActionResult> GetRevenueSummary()
    {
        var result = await _context.Invoices
            .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalRevenue = g.Sum(i => i.Amount)
            }).OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("technician-activity")]
    public async Task<IActionResult> GetTechnicianActivity()
    {
        var result = await _context.Jobs
            .Where(j => j.Status == "Completed")
            .GroupBy(j => j.TechnicianId)
            .Select(g => new
            {
                TechnicianId = g.Key,
                TechnicianName = g.First().Technician.User.FullName,
                JobsCompleted = g.Count()
            }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("job-trends")]
    public async Task<IActionResult> GetJobTrends([FromQuery] int days = 7)
    {
        var fromDate = DateTime.UtcNow.Date.AddDays(-days);

        var trendData = await _context.Jobs
            .Where(j => j.CreatedAt >= fromDate)
            .GroupBy(j => j.CreatedAt.Date)
            .Select(g => new JobTrendDto
            {
                Date = g.Key,
                Scheduled = g.Count(j => j.Status == "Scheduled"),
                Started = g.Count(j => j.Status == "Started"),
                Completed = g.Count(j => j.Status == "Completed")
            })
            .OrderBy(t => t.Date)
            .ToListAsync();

        return Ok(trendData);
    }
}