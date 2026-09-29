namespace Backend.API.Models;

public class EquipmentComponent
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string ComponentType { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string? Specifications { get; set; }
    public DateTime InstalledAt { get; set; }

    public Equipment? Equipment { get; set; }
}