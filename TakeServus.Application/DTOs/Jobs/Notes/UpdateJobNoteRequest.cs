namespace TakeServus.Application.DTOs.Jobs.Notes;

public class UpdateJobNoteRequest
{
    public Guid JobId { get; set; }
    public Guid NoteId { get; set; }
    public string Note { get; set; } = default!;
}