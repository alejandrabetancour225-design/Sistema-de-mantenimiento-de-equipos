namespace Backend.API.DTOs;

public class DashboardOpenIncidentResponse
{
    public Guid Id { get; set; }
    public string EquipmentInternalCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysOpen { get; set; }
    public bool IsUrgent { get; set; }
}
