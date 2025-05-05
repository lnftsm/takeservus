namespace TakeServus.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "Technician";
    public string PasswordHash { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Technician? Technician { get; set; }
}