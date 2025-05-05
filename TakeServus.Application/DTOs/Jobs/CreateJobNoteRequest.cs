namespace TakeServus.Application.DTOs.Jobs;

public class CreateJobNoteRequest
{
    public Guid JobId { get; set; }
    public string Note { get; set; } = default!;
}