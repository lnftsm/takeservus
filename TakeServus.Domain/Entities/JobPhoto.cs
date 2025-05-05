namespace TakeServus.Domain.Entities;
public class JobPhoto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string PhotoUrl { get; set; } = default!;
    public DateTime UploadedAt { get; set; }

    public Job Job { get; set; } = default!;
}