namespace Backend.API.DTOs;

public class MaintenanceReportRow
{
    public string EquipmentInternalCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OtherCosts { get; set; }
    public decimal SparePartsCost { get; set; }
    public decimal TotalCost { get; set; }
    public string ReportedProblem { get; set; } = string.Empty;
}
