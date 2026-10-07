using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Backend.API.Validation;

namespace Backend.API.DTOs;

public class CreateEquipmentComponentRequest
{
    [Required]
    public Guid EquipmentId { get; set; }

    [Required(ErrorMessage = "El tipo de componente es obligatorio.")]
    [StringLength(100, MinimumLength = 1)]
    public string ComponentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(100, MinimumLength = 1)]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(100, MinimumLength = 1)]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de serie es obligatorio.")]
    [StringLength(100, MinimumLength = 1)]
    public string SerialNumber { get; set; } = string.Empty;

    [MaxJsonLength(8000)]
    public JsonElement? Specifications { get; set; }

    [NotInFuture]
    public DateTime? InstalledAt { get; set; }
}
