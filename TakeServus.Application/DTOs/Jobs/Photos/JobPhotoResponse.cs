namespace TakeServus.Application.DTOs.Jobs.Photos;

public class JobPhotoResponse
{
  public Guid Id { get; set; }
  public string PhotoUrl { get; set; } = default!;
  public DateTime UploadedAt { get; set; }
}
