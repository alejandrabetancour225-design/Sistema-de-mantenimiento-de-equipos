using Backend.API.Models;

namespace Backend.API.DTOs;

public class UpdateMaintenanceRequest
{
    public MaintenanceStatus? Status { get; set; }
    public string? ReportedProblem { get; set; }
    public string? WorkDone { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? OtherCosts { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
    public string? Observations { get; set; }
    public Guid? TechnicianId { get; set; }
}
