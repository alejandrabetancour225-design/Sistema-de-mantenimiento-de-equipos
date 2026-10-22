using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class CreatePreventiveFromPredictionRequest
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string ReportedProblem { get; set; } = string.Empty;
}
