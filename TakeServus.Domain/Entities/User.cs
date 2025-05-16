using TakeServus.Domain.Common;

namespace TakeServus.Domain.Entities;

public class User : BaseEntityWithAudit
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "Technician";
    public string PasswordHash { get; set; } = default!;

    public Technician? Technician { get; set; }
}