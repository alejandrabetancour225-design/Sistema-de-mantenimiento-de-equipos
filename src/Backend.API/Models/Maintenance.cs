namespace Backend.API.Models;

public class Maintenance
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid TechnicianId { get; set; }
    public MaintenanceType Type { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.OPEN;
    public string ReportedProblem { get; set; } = string.Empty;
    public string? WorkDone { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OtherCosts { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
    public string Observations { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Equipment? Equipment { get; set; }
    public User? Technician { get; set; }
    public Incident? Incident { get; set; }
    public ICollection<MaintenanceSparePart>? SpareParts { get; set; }
}
