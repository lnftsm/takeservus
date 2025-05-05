using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Users;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Dispatcher,Admin")]
public class UsersController : ControllerBase
{
    private readonly TakeServusDbContext _context;

    public UsersController(TakeServusDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        if (_context.Users.Any(u => u.Email == request.Email))
            return BadRequest("Email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(user.Id);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new UserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber ?? string.Empty,
                Role = u.Role,
                IsActive = u.IsActive
            }).ToListAsync();

        return Ok(users);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUser(UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(request.Id);
        if (user == null) return NotFound();

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = HttpContext.User.FindFirst("UserId")?.Value;
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(Guid.Parse(userId));
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return BadRequest("Old password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok("Password changed successfully.");
    }
}