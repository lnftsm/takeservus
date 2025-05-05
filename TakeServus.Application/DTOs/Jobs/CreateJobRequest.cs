namespace TakeServus.Application.DTOs.Jobs;

public class CreateJobRequest
{
    public Guid CustomerId { get; set; }
    public Guid TechnicianId { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime ScheduledAt { get; set; }
}