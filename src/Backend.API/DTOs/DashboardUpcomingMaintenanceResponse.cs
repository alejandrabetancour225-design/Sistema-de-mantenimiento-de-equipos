namespace Backend.API.DTOs;

public class DashboardUpcomingMaintenanceResponse
{
    public Guid Id { get; set; }
    public string EquipmentInternalCode { get; set; } = string.Empty;
    public DateOnly? NextMaintenanceDate { get; set; }
    public int DaysUntil { get; set; }
    public bool IsOverdue { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
}
