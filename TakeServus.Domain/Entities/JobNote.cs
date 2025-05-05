namespace TakeServus.Domain.Entities;

public class JobNote
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string Note { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public Job Job { get; set; } = default!;
}