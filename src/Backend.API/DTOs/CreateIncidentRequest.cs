namespace Backend.API.DTOs;

public class CreateIncidentRequest
{
    public Guid EquipmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? ReportedAt { get; set; }
    public Guid? MaintenanceId { get; set; }
}