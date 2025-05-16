namespace TakeServus.Application.DTOs.Technicians;
public class TechnicianResponse
{
  public Guid Id { get; set; }
  public string FullName { get; set; } = default!;
  public string Email { get; set; } = default!;
  public string? PhoneNumber { get; set; }

  public decimal? CurrentLatitude { get; set; }
  public decimal? CurrentLongitude { get; set; }
  public bool IsAvailable { get; set; }
}