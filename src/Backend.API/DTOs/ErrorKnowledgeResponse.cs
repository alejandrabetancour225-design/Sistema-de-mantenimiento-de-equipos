using Backend.API.Models;

namespace Backend.API.DTOs;

public class ErrorKnowledgeResponse
{
    public Guid Id { get; set; }
    public string Symptom { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public string PossibleCauses { get; set; } = string.Empty;
    public string SuggestedSolution { get; set; } = string.Empty;
    public ErrorSeverity Severity { get; set; }
    public string Source { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
