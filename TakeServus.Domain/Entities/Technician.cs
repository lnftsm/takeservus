using TakeServus.Domain.Common;

namespace TakeServus.Domain.Entities;

public class Technician : BaseEntityWithAudit
{
    public Guid UserId { get; set; }
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public bool IsAvailable { get; set; } = true;

    public User User { get; set; } = default!;
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}