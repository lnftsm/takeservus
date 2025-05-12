using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Common;
using TakeServus.Application.DTOs.Jobs;
using TakeServus.Application.Interfaces;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using System.Linq.Dynamic.Core;
using TakeServus.Infrastructure.Services;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobController : ControllerBase
{
    private readonly TakeServusDbContext _context;
    //private readonly IQueuedEmailService _queuedEmailService;
    private readonly IFileStorageService _fileStorageService;
    private readonly FirebaseStorageService _firebaseStorageService;

    public JobController(
        TakeServusDbContext context,
        //IQueuedEmailService queuedEmailService,
        IFileStorageService fileStorageService,
        FirebaseStorageService firebaseStorageService)
    {
        _context = context;
        //_queuedEmailService = queuedEmailService;
        _fileStorageService = fileStorageService;
        _firebaseStorageService = firebaseStorageService;
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
        if (request.ScheduledAt < DateTime.UtcNow)
            return BadRequest("Scheduled date cannot be in the past.");
        if (request.ScheduledAt > DateTime.UtcNow.AddDays(30))
            return BadRequest("Scheduled date cannot be more than 30 days in the future.");

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
            if (string.IsNullOrWhiteSpace(technician.User.Email)) return BadRequest("Technician email not found.");

            // TODO: Send email to technician
            // Uncomment the following line to send an email
            // await _queuedEmailService.EnqueueEmailAsync(
            //     technician.User.Email,
            //     "New Job Assigned",
            //     $"You have been assigned a new job: {job.Title} scheduled at {job.ScheduledAt}"
            // );
        }

        return Ok(job.Id);
    }

    [HttpPut("reassign")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> ReassignJob([FromBody] ReassignJobRequest request)
    {
        if (request.JobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
        if (request.NewTechnicianId == Guid.Empty) return BadRequest("New technician ID cannot be empty.");
        if (request.JobId == request.NewTechnicianId)
            return BadRequest("Job ID and new technician ID cannot be the same.");

        var job = await _context.Jobs
            .Include(j => j.Technician)
            .ThenInclude(t => t.User)
            .FirstOrDefaultAsync(j => j.Id == request.JobId && !j.IsDeleted);

        if (job == null) return NotFound("Job not found.");
        if (job.IsDeleted) return BadRequest("Job is archived.");
        if (job.Status != "Scheduled")
            return BadRequest("Job must be in 'Scheduled' status to reassign.");
        if (request.NewTechnicianId == job.TechnicianId)
            return BadRequest("Job is already assigned to the selected technician.");

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

        if (string.IsNullOrWhiteSpace(technician.User.Email)) return BadRequest("Technician email not found.");

        // TODO: Send email to new technician
        // Uncomment the following line to send an email
        // await _queuedEmailService.EnqueueEmailAsync(
        //     technician.User.Email,
        //     "Job Reassigned",
        //     $"You have been reassigned to job: {job.Title} scheduled at {job.ScheduledAt}"
        // );

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
        if (request.JobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest("Status cannot be empty.");
        if (request.Status != "Scheduled" && request.Status != "Started" && request.Status != "Completed")
            return BadRequest("Invalid status. Allowed values are: Scheduled, Started, Completed.");
        if (request.Status == "Started" && request.ScheduledAt < DateTime.UtcNow)
            return BadRequest("Job cannot be started before the scheduled date.");
        if (request.Status == "Completed" && request.ScheduledAt > DateTime.UtcNow)
            return BadRequest("Job cannot be completed before the scheduled date.");
        if (request.Status == "Completed" && request.StartedAt == null)
            return BadRequest("Job must be started before it can be completed.");
        if (request.Status == "Completed" && request.CompletedAt != null)
            return BadRequest("Job is already completed.");
        if (request.Status == "Started" && request.CompletedAt != null)
            return BadRequest("Job is already completed.");
        if (request.Status == "Scheduled" && request.StartedAt != null)
            return BadRequest("Job is already started.");
        if (request.Status == "Scheduled" && request.CompletedAt != null)
            return BadRequest("Job is already completed.");
        if (request.Status == "Started" && request.CompletedAt != null)
            return BadRequest("Job is already completed.");
        var job = await _context.Jobs
            .Include(j => j.Technician)
            .ThenInclude(t => t.User)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == request.JobId && !j.IsDeleted);

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
            // TODO: Send email to new technician
            // Uncomment the following line to send an email
            // await _queuedEmailService.EnqueueEmailAsync(
            //     job.Customer.Email,
            //     $"Job Status Updated: {job.Title}",
            //     $"The status of your job '{job.Title}' has changed from {oldStatus} to {request.Status}."
            // );
        }

        return Ok();
    }

    [HttpDelete("{jobId}")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> ArchiveJob(Guid jobId)
    {
        if (jobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null) return NotFound("Job not found.");
        if (job.IsDeleted) return BadRequest("Job already archived.");

        job.IsDeleted = true;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _context.JobActivities.Add(new JobActivity
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            ActivityType = "Archived",
            Details = $"Job archived by user",
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
        });

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Job archived successfully." });
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
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;
        if (page > 100) page = 100;

        if (string.IsNullOrWhiteSpace(status)) status = null;
        if (string.IsNullOrWhiteSpace(keyword)) keyword = null;
        if (date == null) date = null;

        if (date.HasValue && date.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest("Date cannot be in the past.");
        if (date.HasValue && date.Value > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))
            return BadRequest("Date cannot be more than 30 days in the future.");

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var technician = await _context.Technicians
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == Guid.Parse(userId));

        if (technician == null) return NotFound("Technician not found.");

        var query = _context.Jobs
            .Include(j => j.Customer)
            .Where(j => j.TechnicianId == technician.Id && !j.IsDeleted)
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
        if (totalCount == 0) return Ok(new TakeServus.Application.DTOs.Common.PagedResult<JobResponse>
        {
            Items = new List<JobResponse>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        });

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
        if (request.JobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(request.Note))
            return BadRequest("Note cannot be empty.");
        if (request.Note.Length > 500)
            return BadRequest("Note cannot exceed 500 characters.");
        var job = await _context.Jobs
            .Include(j => j.Technician)
            .ThenInclude(t => t.User)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == request.JobId && !j.IsDeleted);
        if (job == null) return NotFound("Job not found.");

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
        if (request.JobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
        if (request.MaterialId == Guid.Empty) return BadRequest("Material ID cannot be empty.");
        if (request.QuantityUsed <= 0) return BadRequest("Quantity used must be greater than zero.");
        if (request.QuantityUsed > 1000) return BadRequest("Quantity used cannot exceed 1000.");

        var job = await _context.Jobs.FindAsync(request.JobId);
        var material = await _context.Materials.FindAsync(request.MaterialId);

        if (job == null || material == null)
            return NotFound("Job or Material not found.");

        if (job.IsDeleted)
            return BadRequest("Job is archived.");

        if (job.Status != "Started" && job.Status != "Completed")
            return BadRequest("Job must be in 'Started' or 'Completed' status to assign materials.");
        if (material.StockQuantity <= 0)
            return BadRequest("Material is out of stock.");
        if (request.QuantityUsed <= 0)
            return BadRequest("Quantity used must be greater than zero.");
        if (request.QuantityUsed > material.StockQuantity)
            return BadRequest("Quantity used exceeds available stock.");

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
        if (jobId == Guid.Empty)
            return BadRequest("Job ID cannot be empty.");

        if (file == null || file.Length == 0)
            return BadRequest("Uploaded file is empty or missing.");

        if (file.Length > 100 * 1024 * 1024)
            return BadRequest("File size exceeds the 100 MB limit.");

        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null || job.IsDeleted)
            return NotFound("Job not found or has been archived.");

        // 🔁 Upload to Firebase under "jobs/{jobId}"
        var filePath = $"jobs/{jobId}/{Guid.NewGuid()}_{file.FileName}";
        var photoUrl = await _firebaseStorageService.UploadFileAsync(file, filePath);

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
            Details = $"Photo uploaded by {User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value}",
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
        });

        await _context.SaveChangesAsync();

        return Ok(new { photo.PhotoUrl });
    }


    [HttpPost("{jobId}/photo-localserver-upload")]
    [Authorize]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    public async Task<IActionResult> UploadToLocalServerPhoto([FromRoute] Guid jobId, IFormFile file)
    {
        if (jobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
        if (file == null) return BadRequest("File cannot be null");
        if (file.Length > 100 * 1024 * 1024) return BadRequest("File size exceeds the limit of 100 MB.");
        if (file.Length < 1) return BadRequest("File is empty");


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
        if (jobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
        var job = await _context.Jobs
            .Include(j => j.Technician)
            .ThenInclude(t => t.User)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == jobId && !j.IsDeleted);
        if (job == null) return NotFound("Job not found.");
        if (job.IsDeleted) return BadRequest("Job is archived.");


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
            .Where(j => !j.IsDeleted)
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

    [HttpGet("{jobId}/notes")]
    public async Task<IActionResult> GetJobNotes(Guid jobId)
    {
        if (jobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
        var job = await _context.Jobs
            .Include(j => j.Technician)
            .ThenInclude(t => t.User)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == jobId && !j.IsDeleted);
        if (job == null) return NotFound("Job not found.");
        if (job.IsDeleted) return BadRequest("Job is archived.");

        var notes = await _context.JobNotes
            .Where(n => n.JobId == jobId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.Id,
                n.Note,
                n.CreatedAt
            })
            .ToListAsync();

        return Ok(new { items = notes });
    }

    [HttpGet("{jobId}/material")]
    public async Task<IActionResult> GetJobMaterials(Guid jobId)
    {
        var materials = await _context.JobMaterials
            .Where(m => m.JobId == jobId)
            .Include(m => m.Material)
            .Select(m => new
            {
                m.Id,
                m.Material.Name,
                m.QuantityUsed
            })
            .ToListAsync();

        return Ok(new { items = materials });
    }

    [HttpGet("{jobId}/photos")]
    public async Task<IActionResult> GetJobPhotos(Guid jobId)
    {
        if (jobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
        var job = await _context.Jobs
            .Include(j => j.Technician)
            .ThenInclude(t => t.User)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == jobId && !j.IsDeleted);
        if (job == null) return NotFound("Job not found.");
        if (job.IsDeleted) return BadRequest("Job is archived.");

        var photos = await _context.JobPhotos
            .Where(p => p.JobId == jobId)
            .OrderByDescending(p => p.UploadedAt)
            .Select(p => new
            {
                p.Id,
                p.PhotoUrl,
                p.UploadedAt
            })
            .ToListAsync();

        return Ok(new { items = photos });
    }
}
