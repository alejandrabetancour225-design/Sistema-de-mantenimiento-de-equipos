using Backend.API.Models;

namespace Backend.API.DTOs;

public class MaintenanceResponse
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string? EquipmentInternalCode { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public MaintenanceType Type { get; set; }
    public MaintenanceStatus Status { get; set; }
    public string ReportedProblem { get; set; } = string.Empty;
    public string? WorkDone { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OtherCosts { get; set; }
    public decimal SparePartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
    public string Observations { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<MaintenanceSparePartResponse> SpareParts { get; set; } = new();
}
