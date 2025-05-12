namespace TakeServus.Domain.Entities;

public class Job
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid TechnicianId { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public Customer Customer { get; set; } = default!;
    public Technician Technician { get; set; } = default!;
    public ICollection<JobNote> Notes { get; set; } = new List<JobNote>();
    public ICollection<JobPhoto> Photos { get; set; } = new List<JobPhoto>();
    public ICollection<JobMaterial> JobMaterials { get; set; } = new List<JobMaterial>();
    public Invoice? Invoice { get; set; }
    public ICollection<JobActivity> Activities { get; set; } = new List<JobActivity>();
}