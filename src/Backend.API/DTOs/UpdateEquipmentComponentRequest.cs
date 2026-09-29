using System.Text.Json;

namespace Backend.API.DTOs;

public class UpdateEquipmentComponentRequest
{
    public string? ComponentType { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public JsonElement? Specifications { get; set; }
    public DateTime? InstalledAt { get; set; }
}