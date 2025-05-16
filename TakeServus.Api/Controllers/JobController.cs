using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Jobs;
using TakeServus.Application.Interfaces;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using System.Linq.Dynamic.Core;
using TakeServus.Application.DTOs.Jobs.Activities;
using TakeServus.Application.DTOs.Customers;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobController : ControllerBase
{
    private readonly TakeServusDbContext _context;
    private readonly IFirebaseStorageService _firebaseStorageService;
    //private readonly IQueuedEmailService _queuedEmailService;
    //private readonly IFileStorageService _fileStorageService;

    public JobController(
        TakeServusDbContext context,
        IFirebaseStorageService firebaseStorageService
        //IQueuedEmailService queuedEmailService,
        //IFileStorageService fileStorageService,
        )
    {
        _context = context;
        //_queuedEmailService = queuedEmailService;
        //_fileStorageService = fileStorageService;
        _firebaseStorageService = firebaseStorageService;
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> CreateJob(CreateJobRequest request)
    {
        if (request.ScheduledAt < DateTime.UtcNow)
            return BadRequest("Scheduled date cannot be in the past.");
        if (request.ScheduledAt > DateTime.UtcNow.AddDays(30))
            return BadRequest("Scheduled date cannot be more than 30 days in the future.");

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

        var technician = await _context.Technicians.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == job.TechnicianId);

        if (technician != null && !string.IsNullOrWhiteSpace(technician.User.Email))
        {
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

        var job = await _context.Jobs
            .Include(j => j.Technician).ThenInclude(t => t.User)
            .FirstOrDefaultAsync(j => j.Id == request.JobId && !j.IsDeleted);

        if (job == null) return NotFound("Job not found.");
        if (job.Status != "Scheduled") return BadRequest("Job must be 'Scheduled' to reassign.");
        if (request.NewTechnicianId == job.TechnicianId) return BadRequest("Technician is already assigned.");

        var technician = await _context.Technicians.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == request.NewTechnicianId);
        if (technician == null) return NotFound("Technician not found.");

        var oldTechnician = job.Technician.User.FullName;
        var newTechnician = technician.User.FullName;
        job.TechnicianId = request.NewTechnicianId;


        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _context.JobActivities.Add(new JobActivity
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            ActivityType = "Reassigned",
            Details = $"From {oldTechnician} to {newTechnician}",
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

    [HttpPut("{jobId}/status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateJobStatusRequest request)
    {
        var job = await _context.Jobs.Include(j => j.Customer).FirstOrDefaultAsync(j => j.Id == request.JobId && !j.IsDeleted);
        if (job == null) return NotFound();
        if (job.Status == request.Status) return BadRequest("Status is already set.");

        job.Status = request.Status;
        if (request.Status == "Started") job.StartedAt = request.Timestamp;
        if (request.Status == "Completed") job.CompletedAt = request.Timestamp;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _context.JobActivities.Add(new JobActivity
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            ActivityType = "StatusChanged",
            Details = $"Changed to {request.Status}",
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
        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null) return NotFound();
        if (job.IsDeleted) return BadRequest("Already archived.");

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

    // [HttpPost("{jobId}/photo-localserver-upload")]
    // [Authorize]
    // [RequestSizeLimit(100 * 1024 * 1024)]
    // [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    // public async Task<IActionResult> UploadToLocalServerPhoto([FromRoute] Guid jobId, IFormFile file)
    // {
    //     if (jobId == Guid.Empty) return BadRequest("Job ID cannot be empty.");
    //     if (file == null) return BadRequest("File cannot be null");
    //     if (file.Length > 100 * 1024 * 1024) return BadRequest("File size exceeds the limit of 100 MB.");
    //     if (file.Length < 1) return BadRequest("File is empty");


    //     var job = await _context.Jobs.FindAsync(jobId);
    //     if (job == null) return NotFound("Job not found");

    //     var photoUrl = await _fileStorageService.UploadFileAsync(file, "job-photos");

    //     var photo = new JobPhoto
    //     {
    //         Id = Guid.NewGuid(),
    //         JobId = jobId,
    //         PhotoUrl = photoUrl,
    //         UploadedAt = DateTime.UtcNow
    //     };

    //     _context.JobPhotos.Add(photo);

    //     var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    //     _context.JobActivities.Add(new JobActivity
    //     {
    //         Id = Guid.NewGuid(),
    //         JobId = job.Id,
    //         ActivityType = "PhotoUploaded",
    //         Details = $"Photo uploaded by {User!.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value}",
    //         PerformedAt = DateTime.UtcNow,
    //         PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
    //     });

    //     await _context.SaveChangesAsync();

    //     return Ok(new { photo.PhotoUrl });
    // }

    // private async Task<string> SavePhotoToServer(IFormFile file)
    // {
    //     var uploadsFolder = Path.Combine("wwwroot", "photoUpload");
    //     Directory.CreateDirectory(uploadsFolder);

    //     var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
    //     var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

    //     await using var stream = new FileStream(fullPath, FileMode.Create);
    //     await file.CopyToAsync(stream);

    //     return $"/photoUpload/{uniqueFileName}";
    // }



    [HttpGet("admin")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<ActionResult<TakeServus.Application.DTOs.Common.PagedResult<JobResponse>>> SearchJobs(
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

    #region "Job related endpoints, activities, notes, materials, photos"

    [HttpGet("{jobId}/activities")]
    [Authorize]
    // This endpoint retrieves the activities related to a specific job
    // It returns a list of activities including the type, details, performed by, and performed at
    public async Task<IActionResult> GetActivities(Guid jobId)
    {
        (bool flowControl, IActionResult value) = await CheckJob(jobId);
        if (!flowControl)
        {
            return value;
        }

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

    [HttpGet("{jobId}/notes")]
    [Authorize]
    // This endpoint retrieves the notes related to a specific job
    // It returns a list of notes including the note content and created at timestamp
    // It also includes the user who created the note
    public async Task<IActionResult> GetJobNotes(Guid jobId)
    {
        (bool flowControl, IActionResult value) = await CheckJob(jobId);
        if (!flowControl)
        {
            return value;
        }

        var notes = await _context.JobNotes
            .Where(n => n.JobId == jobId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.Id,
                n.Note,
                CreatedAt = n.CreatedAt.ToString("dd-MM-yyyy HH:mm:ss"),
                CreatedBy = n.CreatedByUser.FullName
            })
            .ToListAsync();

        return Ok(new { items = notes });
    }

    [HttpGet("{jobId}/materials")]
    [Authorize]
    // This endpoint retrieves the materials related to a specific job
    // It returns a list of materials including the name, quantity used, unit, unit price, and total cost
    // It also includes the object ID of the JobMaterial
    public async Task<IActionResult> GetJobMaterials(Guid jobId)
    {
        (bool flowControl, IActionResult value) = await CheckJob(jobId);
        if (!flowControl)
        {
            return value;
        }

        var materials = await _context.JobMaterials
            .Where(m => m.JobId == jobId)
            .Include(m => m.Material)
            .Select(m => new
            {
                m.Id,
                m.Material.Name,
                m.QuantityUsed,
                m.Material.Unit,
                m.UnitPrice,
                Total = m.QuantityUsed * m.UnitPrice
            })
            .ToListAsync();

        return Ok(new { items = materials });
    }

    [HttpGet("{jobId}/photos")]
    [Authorize]
    public async Task<IActionResult> GetJobPhotos(Guid jobId)
    {
        (bool flowControl, IActionResult value) = await CheckJob(jobId);
        if (!flowControl)
        {
            return value;
        }

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

    [Authorize]
    [HttpPost("{jobId}/photo-upload")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto([FromRoute] Guid jobId, IFormFile file)
    {
        (bool flowControl, IActionResult value) = await CheckJob(jobId);
        if (!flowControl)
        {
            return value;
        }

        if (file == null || file.Length == 0)
            return BadRequest("Uploaded file is empty or missing.");

        if (file.Length > 100 * 1024 * 1024)
            return BadRequest("File size exceeds the 100 MB limit.");


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
            JobId = jobId,
            ActivityType = "PhotoUploaded",
            Details = $"Photo uploaded by {User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value}",
            PerformedAt = DateTime.UtcNow,
            PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
        });

        await _context.SaveChangesAsync();

        return Ok(new { photo.PhotoUrl });
    }
    #endregion

    #region "Private Methods"
    // This method checks if the job exists and is not deleted
    // It returns a tuple with a boolean indicating if the flow should continue
    // and an IActionResult to return if the job is not found or is deleted
    private async Task<(bool flowControl, IActionResult value)> CheckJob(Guid jobId)
    {
        if (jobId == Guid.Empty) return (false, BadRequest("Job ID cannot be empty."));

        var job = await _context.Jobs
            .Include(j => j.Technician).ThenInclude(t => t.User)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == jobId && !j.IsDeleted);

        return job == null ? (false, NotFound("Job not found.")) : (true, Ok());
    }
    #endregion

    [AllowAnonymous]
    [HttpPost("public/guest")]
    public async Task<IActionResult> CreateGuestJob([FromBody] GuestCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest("Full name is required.");

        if (string.IsNullOrWhiteSpace(request.PhoneNumber) && string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("At least one contact info (email or phone) is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Job title is required.");

        if (request.ScheduledAt < DateTime.UtcNow)
            return BadRequest("Scheduled date cannot be in the past.");

        if (request.ScheduledAt > DateTime.UtcNow.AddDays(30))
            return BadRequest("Scheduled date cannot be more than 30 days in the future.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = "Not Provided",
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        var job = new Job
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Title = request.Title,
            Description = request.Description,
            ScheduledAt = request.ScheduledAt,
            Status = "Scheduled",
            IsAssigned = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Optional logging
        Console.WriteLine($"[GUEST] Job created: {job.Title} for {customer.FullName}");

        // TODO: Queue email notification to dispatcher/manager
        // await _queuedEmailService.EnqueueEmailAsync("dispatcher@example.com", "New Guest Job Request", $"New guest job created: {job.Title}");

        return Ok(new { JobId = job.Id });
    }

    [Authorize]
    [HttpPost("public/registered")]
    public async Task<IActionResult> CreateRegisteredJob([FromBody] RegisteredJobRequest request)
    {
        if (request.CustomerId == Guid.Empty)
            return BadRequest("CustomerId is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Job title is required.");

        if (request.ScheduledAt < DateTime.UtcNow)
            return BadRequest("Scheduled date cannot be in the past.");

        if (request.ScheduledAt > DateTime.UtcNow.AddDays(30))
            return BadRequest("Scheduled date cannot be more than 30 days in the future.");

        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer == null || customer.IsDeleted)
            return NotFound("Customer not found.");

        var job = new Job
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            Title = request.Title,
            Description = request.Description,
            ScheduledAt = request.ScheduledAt,
            Status = "Scheduled",
            IsAssigned = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        Console.WriteLine($"[REGISTERED] Job created: {job.Title} for {customer.FullName}");

        // TODO: Send email to dispatcher
        // await _queuedEmailService.EnqueueEmailAsync("dispatcher@example.com", "New Registered Job", $"New job created: {job.Title} for {customer.FullName}");

        return Ok(new { JobId = job.Id });
    }
}
