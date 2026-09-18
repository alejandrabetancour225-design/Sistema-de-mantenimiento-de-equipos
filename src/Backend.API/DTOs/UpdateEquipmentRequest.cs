using System.Text.Json;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class UpdateEquipmentRequest
{
    public string? InternalCode { get; set; }
    public string? SerialNumber { get; set; }
    public string? Type { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public JsonElement? Characteristics { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public decimal? AcquisitionPrice { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public string? Location { get; set; }
    public EquipmentStatus? Status { get; set; }
}