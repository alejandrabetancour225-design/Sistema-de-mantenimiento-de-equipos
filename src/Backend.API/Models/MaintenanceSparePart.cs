namespace Backend.API.Models;

public class MaintenanceSparePart
{
    public Guid Id { get; set; }
    public Guid MaintenanceId { get; set; }
    public Guid SparePartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCostAtUse { get; set; }

    public Maintenance? Maintenance { get; set; }
    public SparePart? SparePart { get; set; }
}
