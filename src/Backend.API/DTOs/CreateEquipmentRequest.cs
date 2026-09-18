using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class CreateEquipmentRequest
{
    [Required]
    public string InternalCode { get; set; } = string.Empty;

    [Required]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Brand { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    public JsonElement? Characteristics { get; set; }

    public DateOnly? AcquisitionDate { get; set; }

    public decimal? AcquisitionPrice { get; set; }

    public DateOnly? WarrantyUntil { get; set; }

    [Required]
    public string Location { get; set; } = string.Empty;

    public EquipmentStatus? Status { get; set; }
}