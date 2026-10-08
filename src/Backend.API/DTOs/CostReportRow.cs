namespace Backend.API.DTOs;

public class CostReportRow
{
    public string EquipmentInternalCode { get; set; } = string.Empty;
    public int MaintenanceCount { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal TotalSparePartsCost { get; set; }
    public decimal TotalOtherCosts { get; set; }
    public decimal TotalCost { get; set; }
}
