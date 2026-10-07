using System.ComponentModel.DataAnnotations;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class CloseMaintenanceRequest
{
    [Required(ErrorMessage = "Describe el trabajo realizado para cerrar el mantenimiento.")]
    [StringLength(4000, MinimumLength = 5, ErrorMessage = "El trabajo realizado debe tener entre 5 y 4000 caracteres.")]
    public string WorkDone { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "La mano de obra debe estar entre 0 y 999.999.999.")]
    public decimal? LaborCost { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Los otros costos deben estar entre 0 y 999.999.999.")]
    public decimal? OtherCosts { get; set; }

    public DateOnly? NextMaintenanceDate { get; set; }

    [StringLength(2000)]
    public string? Observations { get; set; }

    // No puede ser UNDER_MAINTENANCE. Por defecto AVAILABLE.
    [EnumDataType(typeof(EquipmentStatus))]
    public EquipmentStatus? EquipmentFinalStatus { get; set; }

    // Solo RESOLVED o CLOSED. Por defecto RESOLVED.
    [EnumDataType(typeof(IncidentStatus))]
    public IncidentStatus? IncidentFinalStatus { get; set; }
}
