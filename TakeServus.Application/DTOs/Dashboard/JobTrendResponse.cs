namespace TakeServus.Application.DTOs.Dashboard;

public class JobTrendResponse
{
  public DateTime Date { get; set; }
  public int Scheduled { get; set; }
  public int Started { get; set; }
  public int Completed { get; set; }
}