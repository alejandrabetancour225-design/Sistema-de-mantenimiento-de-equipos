namespace Backend.API.DTOs;

public class EquipmentHistoryResponse
{
    public Guid EquipmentId { get; set; }
    public string? EquipmentInternalCode { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal TotalSparePartsCost { get; set; }
    public decimal TotalOtherCosts { get; set; }
    public decimal TotalCost { get; set; }
    public List<MaintenanceResponse> Maintenances { get; set; } = new();
    public List<IncidentResponse> Incidents { get; set; } = new();
}