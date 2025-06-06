namespace TakeServus.Application.DTOs.Customers;

public class CustomerResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string Address { get; set; } = default!;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDeleted { get; set; }
}