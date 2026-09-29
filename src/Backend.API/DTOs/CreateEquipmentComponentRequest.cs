using System.Text.Json;

namespace Backend.API.DTOs;

public class CreateEquipmentComponentRequest
{
    public Guid EquipmentId { get; set; }
    public string ComponentType { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public JsonElement? Specifications { get; set; }
    public DateTime? InstalledAt { get; set; }
}