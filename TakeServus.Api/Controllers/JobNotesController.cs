using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TakeServus.Application.DTOs.Jobs.Notes;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/job-notes")]
[Authorize]
public class JobNotesController : ControllerBase
{
  private readonly TakeServusDbContext _context;

  public JobNotesController(TakeServusDbContext context)
  {
    _context = context;
  }

  [HttpPost("{jobId}")]
  [Authorize(Roles = "Technician")]
  public async Task<ActionResult<JobNoteResponse>> AddNote(Guid jobId, [FromBody] CreateJobNoteRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Note))
      return BadRequest("Note cannot be empty.");

    var job = await _context.Jobs.FindAsync(jobId);
    if (job == null || job.IsDeleted) return NotFound("Job not found.");

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Unauthorized();

    var note = new JobNote
    {
      Id = Guid.NewGuid(),
      JobId = jobId,
      Note = request.Note,
      CreatedAt = DateTime.UtcNow,
      CreatedByUserId = Guid.Parse(userId)
    };

    _context.JobNotes.Add(note);

    _context.JobActivities.Add(new JobActivity
    {
      Id = Guid.NewGuid(),
      JobId = jobId,
      ActivityType = "NoteAdded",
      Details = $"Note added by {User.FindFirstValue(ClaimTypes.Name)}",
      PerformedAt = DateTime.UtcNow,
      PerformedByUserId = Guid.Parse(userId)
    });

    await _context.SaveChangesAsync();

    return Ok(new JobNoteResponse
    {
      NoteId = note.Id,
      JobId = note.JobId,
      Note = note.Note,
      CreatedAt = note.CreatedAt
    });
  }

  [HttpPut("{noteId}")]
  [Authorize(Roles = "Technician")]
  public async Task<IActionResult> EditNote(Guid noteId, [FromBody] UpdateJobNoteRequest request)
  {
    if (noteId != request.NoteId)
      return BadRequest("Note ID mismatch.");

    var note = await _context.JobNotes.FindAsync(noteId);
    if (note == null)
      return NotFound("Note not found.");

    var jobId = note.JobId;
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Unauthorized();

    note.Note = request.Note;
    note.ModifiedAt = DateTime.UtcNow;
    note.ModifiedByUserId = Guid.Parse(userId);

    _context.JobActivities.Add(new JobActivity
    {
      Id = Guid.NewGuid(),
      JobId = jobId,
      ActivityType = "NoteEdited",
      Details = $"Note edited by {User.FindFirstValue(ClaimTypes.Name)}",
      PerformedAt = DateTime.UtcNow,
      PerformedByUserId = Guid.Parse(userId)
    });

    await _context.SaveChangesAsync();

    return Ok(new JobNoteResponse
    {
      NoteId = note.Id,
      JobId = note.JobId,
      Note = note.Note
    });
  }

  [HttpDelete("{jobId}/note/{noteId}")]
  [Authorize(Roles = "Technician,Owner,Dispatcher")]
  public async Task<IActionResult> DeleteNote(Guid jobId, Guid noteId)
  {
    var note = await _context.JobNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.JobId == jobId);
    if (note == null) return NotFound("Note not found.");

    _context.JobNotes.Remove(note);

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    _context.JobActivities.Add(new JobActivity
    {
      Id = Guid.NewGuid(),
      JobId = jobId,
      ActivityType = "NoteDeleted",
      Details = $"Note deleted by {User.FindFirstValue(ClaimTypes.Name)}",
      PerformedAt = DateTime.UtcNow,
      PerformedByUserId = userId != null ? Guid.Parse(userId) : Guid.Empty
    });

    await _context.SaveChangesAsync();
    return Ok(new { Message = "Note deleted successfully." });
  }
}