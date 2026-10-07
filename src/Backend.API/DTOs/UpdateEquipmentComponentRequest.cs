using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Backend.API.Validation;

namespace Backend.API.DTOs;

public class UpdateEquipmentComponentRequest
{
    [StringLength(100, MinimumLength = 1)]
    public string? ComponentType { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? Brand { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? Model { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? SerialNumber { get; set; }

    [MaxJsonLength(8000)]
    public JsonElement? Specifications { get; set; }

    [NotInFuture]
    public DateTime? InstalledAt { get; set; }
}
