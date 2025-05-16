using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TakeServus.Application.Interfaces;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/job-photos")]
[Authorize]
public class JobPhotosController : ControllerBase
{
  private readonly TakeServusDbContext _context;
  private readonly IFirebaseStorageService _firebaseStorageService;

  public JobPhotosController(
      TakeServusDbContext context,
      IFirebaseStorageService firebaseStorageService)
  {
    _context = context;
    _firebaseStorageService = firebaseStorageService;
  }

  [HttpDelete("{photoId}")]
  [Authorize(Roles = "Technician,Owner,Dispatcher")]
  public async Task<IActionResult> DeletePhoto(Guid photoId)
  {
    var photo = await _context.JobPhotos
        .Include(p => p.Job)
        .FirstOrDefaultAsync(p => p.Id == photoId);

    if (photo == null)
      return NotFound("Photo not found.");

    var jobId = photo.JobId;
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    _context.JobPhotos.Remove(photo);

    _context.JobActivities.Add(new JobActivity
    {
      Id = Guid.NewGuid(),
      JobId = jobId,
      ActivityType = "PhotoDeleted",
      Details = $"Photo deleted by {userName}",
      PerformedAt = DateTime.UtcNow,
      PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
    });

    try
    {
      await _firebaseStorageService.DeleteFile(photo.PhotoUrl);
    }
    catch (Exception ex)
    {
      return StatusCode(500, $"Error deleting from Firebase: {ex.Message}");
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
      Message = "Photo deleted successfully.",
      PhotoId = photo.Id,
      JobId = jobId
    });
  }

  [HttpPost("batch-delete")]
  [Authorize(Roles = "Technician,Owner,Dispatcher")]
  public async Task<IActionResult> BatchDeletePhotos([FromBody] List<Guid> photoIds)
  {
    if (photoIds == null || !photoIds.Any())
      return BadRequest("No photo IDs provided.");

    var photos = await _context.JobPhotos
        .Where(p => photoIds.Contains(p.Id))
        .ToListAsync();

    if (!photos.Any())
      return NotFound("No matching photos found.");

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    foreach (var photo in photos)
    {
      _context.JobPhotos.Remove(photo);

      _context.JobActivities.Add(new JobActivity
      {
        Id = Guid.NewGuid(),
        JobId = photo.JobId,
        ActivityType = "PhotoDeleted",
        Details = $"Batch photo deleted by {userName}",
        PerformedAt = DateTime.UtcNow,
        PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
      });

      try
      {
        await _firebaseStorageService.DeleteFile(photo.PhotoUrl);
      }
      catch (Exception ex)
      {
        // Log and continue
        Console.WriteLine($"Failed to delete photo {photo.Id} from Firebase: {ex.Message}");
      }
    }

    await _context.SaveChangesAsync();

    return Ok(new
    {
      Message = $"{photos.Count} photo(s) deleted successfully.",
      DeletedPhotoIds = photos.Select(p => p.Id)
    });
  }
}