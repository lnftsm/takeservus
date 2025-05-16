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

    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetDashboardSummary()
    {
        var jobs = _context.Jobs.Where(j => !j.IsDeleted);
        var users = _context.Users.Where(u => u.IsActive);
        var technicians = _context.Technicians;
        var materials = _context.Materials;
        var customers = _context.Customers.Where(c => !c.IsDeleted);

        var result = new DashboardSummaryResponse
        {
            TotalJobs = await jobs.CountAsync(),
            ScheduledJobs = await jobs.CountAsync(j => j.Status == "Scheduled"),
            StartedJobs = await jobs.CountAsync(j => j.Status == "Started"),
            CompletedJobs = await jobs.CountAsync(j => j.Status == "Completed"),

            ActiveTechnicians = await users.CountAsync(u => u.Role == "Technician"),
            TotalCustomers = await customers.CountAsync(),

            LowStockMaterials = await materials
                .Where(m => m.StockQuantity < 10)
                .Select(m => new LowStockMaterialResponse
                {
                    MaterialName = m.Name,
                    StockQuantity = m.StockQuantity
                }).ToListAsync()
        };

        return Ok(result);
    }

    [HttpGet("job-summary")]
    public async Task<IActionResult> GetJobSummary()
    {
        var result = await _context.Jobs
            .Where(j => !j.IsDeleted)
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
            })
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("technician-activity")]
    public async Task<IActionResult> GetTechnicianActivity()
    {
        var result = await _context.Jobs
            .Where(j => !j.IsDeleted && j.Status == "Completed")
            .GroupBy(j => j.TechnicianId)
            .Select(g => new
            {
                TechnicianId = g.Key,
                TechnicianName = g.Select(j => j.Technician.User.FullName).FirstOrDefault() ?? "N/A",
                JobsCompleted = g.Count()
            }).ToListAsync();

        return Ok(result);
    }

    [HttpGet("job-trends")]
    public async Task<ActionResult<IEnumerable<JobTrendResponse>>> GetJobTrends()
    {
        var today = DateTime.Today;
        var startDate = today.AddDays(-6);

        var jobs = await _context.Jobs
            .Where(j => !j.IsDeleted &&
                        j.ScheduledAt.HasValue &&
                        j.ScheduledAt.Value.Date >= startDate &&
                        j.ScheduledAt.Value.Date <= today)
            .ToListAsync();

        var trends = Enumerable.Range(0, 7)
            .Select(i => startDate.AddDays(i))
            .Select(date => new JobTrendResponse
            {
                Date = date,
                Scheduled = jobs.Count(j => j.ScheduledAt?.Date == date),
                Started = jobs.Count(j => j.StartedAt?.Date == date),
                Completed = jobs.Count(j => j.CompletedAt?.Date == date)
            }).ToList();

        return Ok(trends);
    }

    [HttpGet("customer-satisfaction")]
    public async Task<IActionResult> GetCustomerSatisfaction()
    {
        var result = await _context.JobFeedbacks
            .Where(f => f.Rating.HasValue)
            .GroupBy(f => f.Rating)
            .Select(g => new
            {
                Rating = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(r => r.Rating)
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("technician-performance")]
    public async Task<IActionResult> GetTechnicianPerformance()
    {
        var technicians = await _context.Technicians
            .Include(t => t.User)
            .ToListAsync();

        var jobGroups = await _context.Jobs
            .Where(j => !j.IsDeleted)
            .Include(j => j.JobFeedbacks)
            .GroupBy(j => j.TechnicianId)
            .ToListAsync();

        var result = jobGroups.Select(g =>
        {
            var technician = technicians.FirstOrDefault(t => t.Id == g.Key);
            var feedbacks = g.SelectMany(j => j.JobFeedbacks).Where(f => f.Rating.HasValue).ToList();

            return new
            {
                TechnicianId = g.Key,
                TechnicianName = technician?.User.FullName ?? "N/A",
                JobsCompleted = g.Count(j => j.Status == "Completed"),
                AverageRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating!.Value), 2) : 0
            };
        }).ToList();

        return Ok(result);
    }
}