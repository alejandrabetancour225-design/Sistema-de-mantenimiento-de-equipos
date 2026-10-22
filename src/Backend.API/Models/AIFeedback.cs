namespace Backend.API.Models;

public class AIFeedback
{
    public Guid Id { get; set; }
    public Guid DiagnosisId { get; set; }
    public Guid TechnicianId { get; set; }

    public string PredictedCause { get; set; } = string.Empty;

    public string ActualCause { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navegaciones
    public AIDiagnosis? Diagnosis { get; set; }
    public User? Technician { get; set; }
}
