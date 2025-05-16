namespace TakeServus.Application.DTOs.Exports;

public class ExportJobRequest
{
  public string Format { get; set; } = "pdf"; // or "excel"
  public Guid JobId { get; set; }
}