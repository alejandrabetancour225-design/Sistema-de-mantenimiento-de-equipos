namespace Backend.API.DTOs;

public class AddSparePartToMaintenanceRequest
{
    public Guid SparePartId { get; set; }
    public int Quantity { get; set; }
}
