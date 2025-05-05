namespace TakeServus.Domain.Entities;

public class Technician
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public bool IsAvailable { get; set; } = true;

    public User User { get; set; } = default!;
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}