using System.ComponentModel.DataAnnotations;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class UpdateErrorKnowledgeRequest
{
    [StringLength(500, MinimumLength = 3, ErrorMessage = "El síntoma debe tener entre 3 y 500 caracteres.")]
    public string? Symptom { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public string? ComponentType { get; set; }

    [StringLength(2000, MinimumLength = 3)]
    public string? PossibleCauses { get; set; }

    [StringLength(2000, MinimumLength = 3)]
    public string? SuggestedSolution { get; set; }

    [EnumDataType(typeof(ErrorSeverity))]
    public ErrorSeverity? Severity { get; set; }

    public bool? Active { get; set; }
}
