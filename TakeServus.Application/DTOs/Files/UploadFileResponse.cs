namespace TakeServus.Application.DTOs.Files;

public class UploadFileResponse
{
  public string Url { get; set; } = default!;
  public string FileName { get; set; } = default!;
}