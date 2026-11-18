using System.ComponentModel.DataAnnotations;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class CreateErrorKnowledgeRequest
{
    [Required(ErrorMessage = "El síntoma es obligatorio.")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "El síntoma debe tener entre 3 y 500 caracteres.")]
    public string Symptom { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de componente es obligatorio.")]
    [StringLength(100, MinimumLength = 2)]
    public string ComponentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Las causas posibles son obligatorias.")]
    [StringLength(2000, MinimumLength = 3)]
    public string PossibleCauses { get; set; } = string.Empty;

    [Required(ErrorMessage = "La solución sugerida es obligatoria.")]
    [StringLength(2000, MinimumLength = 3)]
    public string SuggestedSolution { get; set; } = string.Empty;

    [EnumDataType(typeof(ErrorSeverity))]
    public ErrorSeverity Severity { get; set; }
}
