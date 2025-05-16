namespace TakeServus.Application.DTOs.Jobs.Activities;

public class JobActivityResponse
{
    public Guid Id { get; set; }
    public string ActivityType { get; set; } = default!;
    public string? Details { get; set; }
    public string PerformedBy { get; set; } = default!;
    public DateTime PerformedAt { get; set; }
}