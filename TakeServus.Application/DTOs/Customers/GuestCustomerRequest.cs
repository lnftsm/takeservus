namespace TakeServus.Application.DTOs.Customers;

public class GuestCustomerRequest
{
  public string FullName { get; set; } = default!;
  public string? PhoneNumber { get; set; }
  public string? Email { get; set; }
  public string Title { get; set; } = default!;
  public string? Description { get; set; }
  public DateTime ScheduledAt { get; set; }
  public decimal? Latitude { get; set; }
  public decimal? Longitude { get; set; }
}