using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Backend.API.Validation;

namespace Backend.API.DTOs;

// Reemplazo completo de los datos del equipo (PUT): los campos opcionales en null se vacían.
// Así se pueden corregir datos mal ingresados, incluido borrar una fecha o un precio.
// El estado NO se cambia aquí: se cambia con PatchEquipmentStatus, que aplica la regla R4.
public class UpdateEquipmentRequest
{
    [Required(ErrorMessage = "El código interno es obligatorio.")]
    [StringLength(50, MinimumLength = 1)]
    public string InternalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de serie es obligatorio.")]
    [StringLength(100, MinimumLength = 1)]
    public string SerialNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo es obligatorio.")]
    [StringLength(100, MinimumLength = 1)]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(100, MinimumLength = 1)]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(100, MinimumLength = 1)]
    public string Model { get; set; } = string.Empty;

    [MaxJsonLength(8000)]
    public JsonElement? Characteristics { get; set; }

    [StringLength(100)]
    public string? OperatingSystem { get; set; }

    public DateOnly? AcquisitionDate { get; set; }

    [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "El precio de compra debe estar entre 0 y 9.999.999.999.")]
    public decimal? AcquisitionPrice { get; set; }

    public DateOnly? WarrantyUntil { get; set; }

    [Required(ErrorMessage = "La ubicación es obligatoria.")]
    [StringLength(200, MinimumLength = 1)]
    public string Location { get; set; } = string.Empty;
}
