namespace TakeServus.Application.DTOs.Jobs.Photos;

public class DeleteJobPhotoRequest
{
  public Guid PhotoId { get; set; }
  public Guid JobId { get; set; }
}