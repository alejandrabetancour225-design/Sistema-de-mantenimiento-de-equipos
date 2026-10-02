using Backend.API.Models;

namespace Backend.API.DTOs;

public class CloseMaintenanceRequest
{
    public string WorkDone { get; set; } = string.Empty;
    public decimal? LaborCost { get; set; }
    public decimal? OtherCosts { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
    public string? Observations { get; set; }
    public EquipmentStatus? EquipmentFinalStatus { get; set; }
    public IncidentStatus? IncidentFinalStatus { get; set; }
}
