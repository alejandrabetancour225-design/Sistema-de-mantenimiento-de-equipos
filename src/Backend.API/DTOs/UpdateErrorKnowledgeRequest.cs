using Backend.API.Models;

namespace Backend.API.DTOs;

public class UpdateErrorKnowledgeRequest
{
    public string? Symptom { get; set; }
    public string? ComponentType { get; set; }
    public string? PossibleCauses { get; set; }
    public string? SuggestedSolution { get; set; }
    public ErrorSeverity? Severity { get; set; }
    public bool? Active { get; set; }
}
