namespace Backend.API.Models;

public class Equipment
{
    public Guid Id { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Characteristics { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public decimal? AcquisitionPrice { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public string Location { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.AVAILABLE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}