namespace TakeServus.Application.DTOs.Users;

public class UpdateUserRequest
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Email { get; set; }
    public string Role { get; set; } = default!;
    public bool IsActive { get; set; }
}