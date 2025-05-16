using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TakeServus.Application.DTOs.Common;
using TakeServus.Application.DTOs.Customers;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly TakeServusDbContext _context;

    public CustomerController(TakeServusDbContext context)
    {
        _context = context;
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<CustomerResponse>>> SearchCustomers(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var customers = _context.Customers
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            customers = customers.Where(c =>
                c.FullName.Contains(query) ||
                (c.Email != null && c.Email.Contains(query)) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(query)));
        }

        var totalCount = await customers.CountAsync();

        var items = await customers
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerResponse
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email ?? string.Empty,
                PhoneNumber = c.PhoneNumber ?? string.Empty
            }).ToListAsync();

        return Ok(new PagedResult<CustomerResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomerById(Guid id)
    {
        var customer = await _context.Customers
            .Where(c => !c.IsDeleted && c.Id == id)
            .FirstOrDefaultAsync();

        if (customer == null)
            return NotFound();

        return Ok(new CustomerResponse
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email ?? string.Empty,
            PhoneNumber = customer.PhoneNumber ?? string.Empty
        });
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedByUserId = Guid.Parse(userId!)
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, customer.Id);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _context.Customers
            .Where(c => !c.IsDeleted && c.Id == id)
            .FirstOrDefaultAsync();

        if (customer == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        customer.FullName = request.FullName;
        customer.Email = request.Email;
        customer.PhoneNumber = request.PhoneNumber;
        customer.Address = request.Address;
        customer.Latitude = request.Latitude;
        customer.Longitude = request.Longitude;
        customer.ModifiedByUserId = Guid.Parse(userId!);
        customer.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> ArchiveCustomer(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null || customer.IsDeleted)
            return NotFound();

        customer.IsDeleted = true;
        customer.ModifiedByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        customer.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> RestoreCustomer(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return NotFound();

        customer.IsDeleted = false;
        customer.ModifiedByUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        customer.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }
}