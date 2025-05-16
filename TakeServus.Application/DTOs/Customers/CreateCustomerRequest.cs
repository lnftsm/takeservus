namespace TakeServus.Application.DTOs.Customers;

public class CreateCustomerRequest
{
    public string FullName { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; } // Optional for guest
    public string Address { get; set; } = default!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
