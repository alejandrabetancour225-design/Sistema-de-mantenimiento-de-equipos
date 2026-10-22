using Backend.API.Models;

namespace Backend.API.DTOs;

public class CreateErrorKnowledgeRequest
{
    public string Symptom { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public string PossibleCauses { get; set; } = string.Empty;
    public string SuggestedSolution { get; set; } = string.Empty;
    public ErrorSeverity Severity { get; set; }
}
