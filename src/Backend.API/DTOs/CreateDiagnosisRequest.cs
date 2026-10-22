using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class CreateDiagnosisRequest
{
    [Required]
    public Guid EquipmentId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Symptom { get; set; } = string.Empty;

    /// <summary>Opcional: vincular a una incidencia existente.</summary>
    public Guid? IncidentId { get; set; }
}
