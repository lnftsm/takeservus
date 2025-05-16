namespace TakeServus.Application.DTOs.Jobs;

public class UpdateJobStatusRequest
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = default!; // Scheduled, EnRoute, Started, Completed
    public DateTime? Timestamp { get; set; }

}