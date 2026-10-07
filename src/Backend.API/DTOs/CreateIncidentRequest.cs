using System.ComponentModel.DataAnnotations;
using Backend.API.Validation;

namespace Backend.API.DTOs;

public class CreateIncidentRequest
{
    [Required]
    public Guid EquipmentId { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "La descripción debe tener entre 5 y 2000 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [NotInFuture]
    public DateTime? ReportedAt { get; set; }

    // Se ignora: el vínculo con el mantenimiento lo crea el flujo de mantenimiento.
    public Guid? MaintenanceId { get; set; }
}
