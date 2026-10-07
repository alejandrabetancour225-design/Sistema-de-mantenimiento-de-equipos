using System.ComponentModel.DataAnnotations;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class UpdateIncidentRequest
{
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "La descripción debe tener entre 5 y 2000 caracteres.")]
    public string? Description { get; set; }

    [EnumDataType(typeof(IncidentStatus))]
    public IncidentStatus? Status { get; set; }

    // Se ignora: el vínculo con el mantenimiento lo crea el flujo de mantenimiento.
    public Guid? MaintenanceId { get; set; }
}
