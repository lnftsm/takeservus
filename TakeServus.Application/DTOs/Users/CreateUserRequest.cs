namespace TakeServus.Application.DTOs.Users;

public class CreateUserRequest
{
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Role { get; set; } = "Technician";
    public string Password { get; set; } = default!;
}