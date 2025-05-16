namespace TakeServus.Application.DTOs.Jobs.Notes;

public class JobNoteResponse
{
    public Guid NoteId { get; set; }

    public Guid JobId { get; set; }

    public string Note { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}