namespace TakeServus.Application.DTOs.Dashboard;

public class DashboardSummaryResponse
{
    public int TotalJobs { get; set; }
    public int ScheduledJobs { get; set; }
    public int EnRouteJobs { get; set; }
    public int StartedJobs { get; set; }
    public int CompletedJobs { get; set; }

    public int ActiveTechnicians { get; set; }
    public int TotalCustomers { get; set; }

    public List<LowStockMaterialDto> LowStockMaterials { get; set; } = new();
}