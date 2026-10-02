using Backend.API.Models;

namespace Backend.API.DTOs;

public class CreateMaintenanceRequest
{
    public Guid EquipmentId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid TechnicianId { get; set; }
    public MaintenanceType Type { get; set; }
    public string ReportedProblem { get; set; } = string.Empty;
    public decimal LaborCost { get; set; }
    public decimal OtherCosts { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
    public string Observations { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
}
