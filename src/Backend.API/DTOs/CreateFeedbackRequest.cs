using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class CreateFeedbackRequest
{
    [Required]
    public Guid DiagnosisId { get; set; }

    [Required]
    [StringLength(500)]
    public string PredictedCause { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string ActualCause { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }
}
