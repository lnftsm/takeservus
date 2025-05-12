using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Common;
using TakeServus.Application.DTOs.Customers;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
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
        var search = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            search = search.Where(c =>
                c.FullName.Contains(query) ||
                c.Email != null && c.Email.Contains(query) ||
                c.PhoneNumber != null && c.PhoneNumber.Contains(query));
        }

        var totalCount = await search.CountAsync();

        var results = await search
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
            Items = results,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomerById(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

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
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, customer.Id);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        customer.FullName = request.FullName;
        customer.Email = request.Email;
        customer.PhoneNumber = request.PhoneNumber;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Owner,Dispatcher")]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}