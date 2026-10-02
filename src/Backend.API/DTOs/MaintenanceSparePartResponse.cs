namespace Backend.API.DTOs;

public class MaintenanceSparePartResponse
{
    public Guid Id { get; set; }
    public Guid SparePartId { get; set; }
    public string SparePartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCostAtUse { get; set; }
    public decimal Subtotal { get; set; }
}
