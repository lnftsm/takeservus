using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Common;
using TakeServus.Application.DTOs.Jobs;
using TakeServus.Application.Interfaces;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using System.Linq.Dynamic.Core;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobController : ControllerBase
{
    private readonly TakeServusDbContext _context;
    private readonly IQueuedEmailService _queuedEmailService;
    private readonly IFileStorageService _fileStorageService;

    public JobController(
        TakeServusDbContext context,
        IQueuedEmailService queuedEmailService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _queuedEmailService = queuedEmailService;
        _fileStorageService = fileStorageService;
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> CreateJob(CreateJobRequest request)
    {
        var job = new Job
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            TechnicianId = request.TechnicianId,
            Title = request.Title,
            Description = request.Description,
            ScheduledAt = request.ScheduledAt,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var activity = new JobActivity
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            ActivityType = "JobCreated",
            Details = $"Job created with title: {request.Title}",
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
        };

        _context.JobActivities.Add(activity);
        await _context.SaveChangesAsync();

        var technician = await _context.Technicians
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == job.TechnicianId);

        if (technician != null)
        {
            await _queuedEmailService.EnqueueEmailAsync(
                technician.User.Email,
                "New Job Assigned",
                $"You have been assigned a new job: {job.Title} scheduled at {job.ScheduledAt}"
            );
        }

        return Ok(job.Id);
    }

    [HttpPut("reassign")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> ReassignJob([FromBody] ReassignJobRequest request)
    {
        var job = await _context.Jobs
            .Include(j => j.Technician)
            .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(j => j.Id == request.JobId);

        if (job == null) return NotFound("Job not found.");

        var technician = await _context.Technicians
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == request.NewTechnicianId);

        if (technician == null) return NotFound("Technician not found.");

        var oldTechnicianName = job.Technician.User.FullName;
        var newTechnicianName = technician.User.FullName;

        job.TechnicianId = request.NewTechnicianId;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _context.JobActivities.Add(new JobActivity
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            ActivityType = "Reassigned",
            Details = $"From {oldTechnicianName} to {newTechnicianName}",
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
        });

        await _context.SaveChangesAsync();

        await _queuedEmailService.EnqueueEmailAsync(
            technician.User.Email,
            "Job Reassigned",
            $"You have been reassigned to job: {job.Title} scheduled at {job.ScheduledAt}"
        );

        return Ok(new
        {
            Message = "Job reassigned successfully.",
            JobId = job.Id,
            TechnicianName = technician.User.FullName
        });
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateJobStatusRequest request)
    {
        var job = await _context.Jobs
            .Include(j => j.Technician)
            .ThenInclude(t => t.User)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == request.JobId);

        if (job == null) return NotFound();
        if (job.Status == request.Status) return BadRequest("Already in requested status.");

        var oldStatus = job.Status;
        job.Status = request.Status;

        if (request.Status == "Started") job.StartedAt = DateTime.UtcNow;
        if (request.Status == "Completed") job.CompletedAt = DateTime.UtcNow;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        _context.JobActivities.Add(new JobActivity
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            ActivityType = "StatusChanged",
            Details = $"From {oldStatus} to {request.Status}",
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
        });

        await _context.SaveChangesAsync();

        // Send status update email to customer
        if (!string.IsNullOrWhiteSpace(job.Customer.Email))
        {
            await _queuedEmailService.EnqueueEmailAsync(
                job.Customer.Email,
                $"Job Status Updated: {job.Title}",
                $"The status of your job '{job.Title}' has changed from {oldStatus} to {request.Status}."
            );
        }

        return Ok();
    }

    [HttpGet("my")]
    [Authorize(Roles = "Technician")]
    public async Task<ActionResult<TakeServus.Application.DTOs.Common.PagedResult<JobResponse>>> GetMyJobs(
        [FromQuery] string? status,
        [FromQuery] DateOnly? date,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var technician = await _context.Technicians
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == Guid.Parse(userId));

        if (technician == null) return NotFound("Technician not found.");

        var query = _context.Jobs
            .Include(j => j.Customer)
            .Where(j => j.TechnicianId == technician.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(j => j.Status == status);

        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            var end = date.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(j => j.ScheduledAt >= start && j.ScheduledAt <= end);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(j =>
                j.Title.Contains(keyword) ||
                j.Description != null && j.Description.Contains(keyword) ||
                j.Customer.FullName.Contains(keyword));
        }

        var totalCount = await query.CountAsync();

        var jobs = await query
            .OrderByDescending(j => j.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JobResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Status = j.Status,
                ScheduledAt = j.ScheduledAt,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                TechnicianName = j.Technician.User.FullName,
                CustomerName = j.Customer.FullName
            }).ToListAsync();

        return Ok(new TakeServus.Application.DTOs.Common.PagedResult<JobResponse>
        {
            Items = jobs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost("note")]
    public async Task<IActionResult> AddNote(CreateJobNoteRequest request)
    {
        var note = new JobNote
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        _context.JobNotes.Add(note);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _context.JobActivities.Add(new JobActivity
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            ActivityType = "NoteAdded",
            Details = $"Note added: {request.Note}",
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
        });
        await _context.SaveChangesAsync();

        return Ok(note.Id);
    }

    [HttpPost("material")]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> AddMaterial(JobAssignMaterialRequest request)
    {
        var job = await _context.Jobs.FindAsync(request.JobId);
        var material = await _context.Materials.FindAsync(request.MaterialId);

        if (job == null || material == null)
            return NotFound("Job or Material not found.");

        if (material.StockQuantity < request.QuantityUsed)
            return BadRequest("Not enough stock available.");

        var jobMaterial = new JobMaterial
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            MaterialId = material.Id,
            QuantityUsed = request.QuantityUsed
        };

        material.StockQuantity -= request.QuantityUsed;

        _context.JobMaterials.Add(jobMaterial);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _context.JobActivities.Add(new JobActivity
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            ActivityType = "MaterialAssigned",
            Details = $"Material {material.Name} assigned with quantity {request.QuantityUsed}",
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
        });


        await _context.SaveChangesAsync();

        return Ok();
    }


    [HttpPost("{jobId}/photo-upload")]
    [Authorize]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto([FromRoute] Guid jobId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null) return NotFound("Job not found");

        var photoUrl = await _fileStorageService.UploadFileAsync(file, "job-photos");

        var photo = new JobPhoto
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            PhotoUrl = photoUrl,
            UploadedAt = DateTime.UtcNow
        };

        _context.JobPhotos.Add(photo);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        _context.JobActivities.Add(new JobActivity
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            ActivityType = "PhotoUploaded",
            Details = $"Photo uploaded by {User!.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value}",
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
        });

        await _context.SaveChangesAsync();

        return Ok(new { photo.PhotoUrl });
    }

    private async Task<string> SavePhotoToServer(IFormFile file)
    {
        var uploadsFolder = Path.Combine("wwwroot", "photoUpload");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/photoUpload/{uniqueFileName}";
    }

    [HttpGet("{jobId}/activity")]
    public async Task<IActionResult> GetActivity(Guid jobId)
    {
        var activities = await _context.JobActivities
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.PerformedAt)
            .Select(a => new JobActivityResponse
            {
                Id = a.Id,
                ActivityType = a.ActivityType,
                Details = a.Details,
                PerformedBy = a.PerformedByUser.FullName,
                PerformedAt = a.PerformedAt
            }).ToListAsync();

        return Ok(activities);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<ActionResult<TakeServus.Application.DTOs.Common.PagedResult<JobResponse>>> GetAllJobs(
       [FromQuery] string? status,
       [FromQuery] string? technicianName,
       [FromQuery] string? customerName,
       [FromQuery] DateOnly? date,
       [FromQuery] string? sortBy,
       [FromQuery] bool desc = true,
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 10)
    {
        var query = _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.Technician)
            .ThenInclude(t => t.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(j => j.Status == status);

        if (!string.IsNullOrWhiteSpace(technicianName))
            query = query.Where(j => j.Technician.User.FullName.Contains(technicianName));

        if (!string.IsNullOrWhiteSpace(customerName))
            query = query.Where(j => j.Customer.FullName.Contains(customerName));

        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            var end = date.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(j => j.ScheduledAt >= start && j.ScheduledAt <= end);
        }

        var totalCount = await query.CountAsync();

        string sortProperty = !string.IsNullOrWhiteSpace(sortBy) ? sortBy : "ScheduledAt";
        string sortOrder = desc ? "descending" : "ascending";

        var jobs = await query
            .OrderBy($"{sortProperty} {sortOrder}")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JobResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                Status = j.Status,
                ScheduledAt = j.ScheduledAt,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                TechnicianName = j.Technician.User.FullName,
                CustomerName = j.Customer.FullName
            }).ToListAsync();

        return Ok(new TakeServus.Application.DTOs.Common.PagedResult<JobResponse>
        {
            Items = jobs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}
