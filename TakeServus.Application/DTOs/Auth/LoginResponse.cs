namespace TakeServus.Application.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Role { get; set; } = default!;
}