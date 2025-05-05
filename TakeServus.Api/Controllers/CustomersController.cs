using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeServus.Application.DTOs.Customers;
using TakeServus.Domain.Entities;
using TakeServus.Persistence.DbContexts;

namespace TakeServus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner,Dispatcher")]
public class CustomersController : ControllerBase
{
    private readonly TakeServusDbContext _context;

    public CustomersController(TakeServusDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return Ok(customer.Id);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetAll()
    {
        var list = await _context.Customers
            .Select(c => new CustomerResponse
            {
                Id = c.Id,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber ?? string.Empty,
                Email = c.Email ?? string.Empty,
                Address = c.Address
            }).ToListAsync();

        return Ok(list);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCustomerRequest request)
    {
        var customer = await _context.Customers.FindAsync(request.Id);
        if (customer == null) return NotFound();

        customer.FullName = request.FullName;
        customer.PhoneNumber = request.PhoneNumber;
        customer.Email = request.Email;
        customer.Address = request.Address;

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("search")]
public async Task<ActionResult<IEnumerable<CustomerResponse>>> SearchCustomers([FromQuery] string query)
{
    if (string.IsNullOrWhiteSpace(query))
        return BadRequest("Query is required.");

    var loweredQuery = query.ToLower();

    var results = await _context.Customers
        .Where(c =>
            (c.FullName != null && c.FullName.ToLower().Contains(loweredQuery)) ||
            (c.Email != null && c.Email.ToLower().Contains(loweredQuery)) ||
            (c.PhoneNumber != null && c.PhoneNumber.Contains(query)))
        .Select(c => new CustomerResponse
        {
            Id = c.Id,
            FullName = c.FullName,
            PhoneNumber = c.PhoneNumber ?? string.Empty,
            Email = c.Email ?? string.Empty,
            Address = c.Address
        }).ToListAsync();

    return Ok(results);
}
}