namespace TakeServus.Application.DTOs.Jobs.Activities;

public class CreateJobActivityRequest
{
    public Guid JobId { get; set; }
    public string ActivityType { get; set; } = default!;
    public string? Details { get; set; }
}