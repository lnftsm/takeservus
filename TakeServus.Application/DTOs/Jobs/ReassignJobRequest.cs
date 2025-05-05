namespace TakeServus.Application.DTOs.Jobs;

public class ReassignJobRequest
{
    public Guid JobId { get; set; }
    public Guid NewTechnicianId { get; set; }
}