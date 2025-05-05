namespace TakeServus.Domain.Entities;

public class JobActivity
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string ActivityType { get; set; } = default!; // e.g. Created, Reassigned, StatusChanged, MaterialUsed, NoteAdded, InvoiceGenerated
    public string? Details { get; set; } // optional description or JSON
    public Guid PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; }

    public User PerformedByUser { get; set; } = default!;
    public Job Job { get; set; } = default!;
}