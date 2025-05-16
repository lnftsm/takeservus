using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Auth;
using TakeServus.Application.DTOs.Common;
using TakeServus.Application.DTOs.Users;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;
using System.Linq.Dynamic.Core;

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

        await RegisterTechnicianAsync(request, user);
        return Ok(user.Id);
    }

    private async Task RegisterTechnicianAsync(CreateUserRequest request, User user)
    {
        if (request.Role == "Technician")
        {
            bool alreadyExists = await _context.Technicians.AnyAsync(t => t.UserId == user.Id);
            if (alreadyExists) return;

            var technician = new Technician
            {
                UserId = user.Id,
                CurrentLatitude = 0,
                CurrentLongitude = 0,
                IsAvailable = false
            };

            _context.Technicians.Add(technician);
            await _context.SaveChangesAsync();
        }
    }

    [HttpGet("list")]
    public async Task<ActionResult<TakeServus.Application.DTOs.Common.PagedResult<UserResponse>>> GetUsers(
          [FromQuery] string? role,
          [FromQuery] string? keyword,
          [FromQuery] string? sortBy,
          [FromQuery] bool desc = true,
          [FromQuery] int page = 1,
          [FromQuery] int pageSize = 10)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u =>
                u.FullName.Contains(keyword) ||
                u.Email.Contains(keyword));
        }

        var totalCount = await query.CountAsync();

        string sortProperty = !string.IsNullOrWhiteSpace(sortBy) ? sortBy : "FullName";
        string sortOrder = desc ? "descending" : "ascending";

        var users = await query
            .OrderBy($"{sortProperty} {sortOrder}")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role
            }).ToListAsync();

        return Ok(new TakeServus.Application.DTOs.Common.PagedResult<UserResponse>
        {
            Items = users,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUser(UpdateUserRequest request)
    {
        if (request.Id == Guid.Empty)
            return BadRequest("User ID is required.");
        if (_context.Users.Any(u => u.Email == request.Email && u.Id != request.Id))
            return BadRequest("Another user with this email already exists.");

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

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest("Old password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok("Password changed successfully.");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok("User deactivated.");
    }
}