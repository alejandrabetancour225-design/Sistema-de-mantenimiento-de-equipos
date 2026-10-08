namespace Backend.API.DTOs;

public class DashboardEquipmentSummaryResponse
{
    public int Available { get; set; }
    public int InUse { get; set; }
    public int UnderMaintenance { get; set; }
    public int OutOfService { get; set; }
    public int Decommissioned { get; set; }
    public int Total { get; set; }
}
