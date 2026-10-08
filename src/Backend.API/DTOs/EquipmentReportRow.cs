namespace Backend.API.DTOs;

public class EquipmentReportRow
{
    public string InternalCode { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? AcquisitionPrice { get; set; }
    public string? AssignedTo { get; set; }
}
