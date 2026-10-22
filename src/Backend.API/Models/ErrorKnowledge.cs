namespace Backend.API.Models;

public class ErrorKnowledge
{
    public Guid Id { get; set; }

    public string Symptom { get; set; } = string.Empty;

    public string ComponentType { get; set; } = string.Empty;

    public string PossibleCauses { get; set; } = string.Empty;

    public string SuggestedSolution { get; set; } = string.Empty;

    public ErrorSeverity Severity { get; set; }

    public string Source { get; set; } = "Manual";

    public int Confidence { get; set; } = 100;

    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}
