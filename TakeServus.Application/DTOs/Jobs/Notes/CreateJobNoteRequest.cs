namespace TakeServus.Application.DTOs.Jobs.Notes;

public class CreateJobNoteRequest
{
    public Guid JobId { get; set; }
    public string Note { get; set; } = default!;
}