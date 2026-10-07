using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Backend.API.Models;
using Backend.API.Validation;

namespace Backend.API.DTOs;

public class UpdateEquipmentRequest
{
    [StringLength(50, MinimumLength = 1)]
    public string? InternalCode { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? SerialNumber { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? Type { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? Brand { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? Model { get; set; }

    [MaxJsonLength(8000)]
    public JsonElement? Characteristics { get; set; }

    public DateOnly? AcquisitionDate { get; set; }

    [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "El precio de compra debe estar entre 0 y 9.999.999.999.")]
    public decimal? AcquisitionPrice { get; set; }

    public DateOnly? WarrantyUntil { get; set; }

    [StringLength(200, MinimumLength = 1)]
    public string? Location { get; set; }

    [EnumDataType(typeof(EquipmentStatus))]
    public EquipmentStatus? Status { get; set; }
}
