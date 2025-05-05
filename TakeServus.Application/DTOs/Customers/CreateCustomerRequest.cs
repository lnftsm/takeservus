namespace TakeServus.Application.DTOs.Customers;

public class CreateCustomerRequest
{
    public string FullName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Address { get; set; } = default!;
}