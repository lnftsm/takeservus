namespace TakeServus.Application.DTOs.Jobs;

public class JobResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Status { get; set; } = default!;
    public string TechnicianName { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}