using TakeServus.Domain.Common;

namespace TakeServus.Domain.Entities;
public class Customer : BaseEntityWithAudit
{
    public string FullName { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string Address { get; set; } = default!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
