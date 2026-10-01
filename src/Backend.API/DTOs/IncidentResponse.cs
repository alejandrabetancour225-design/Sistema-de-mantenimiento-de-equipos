using Backend.API.Models;

namespace Backend.API.DTOs;

public class IncidentResponse
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string? EquipmentInternalCode { get; set; }
    public Guid ReportedBy { get; set; }
    public string? ReportedByName { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public IncidentStatus Status { get; set; }
    public Guid? MaintenanceId { get; set; }
}