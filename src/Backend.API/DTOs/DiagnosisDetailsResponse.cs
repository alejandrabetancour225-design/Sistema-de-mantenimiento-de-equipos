using System.Text.Json.Serialization;

namespace Backend.API.DTOs;

public class DiagnosisDetailsResponse
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string EquipmentInternalCode { get; set; } = string.Empty;
    public Guid? IncidentId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;

    public string Symptom { get; set; } = string.Empty;
    public List<DiagnosisRankedCause> RankedCauses { get; set; } = new();
    public bool RequiresTechnician { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public DiagnosisContextSnapshot ContextSnapshot { get; set; } = new();
}

public class DiagnosisRankedCause
{
    [JsonPropertyName("cause")]
    public string Cause { get; set; } = string.Empty;

    [JsonPropertyName("probability")]
    public double Probability { get; set; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;
}

public class DiagnosisContextSnapshot
{
    [JsonPropertyName("brand")]
    public string Brand { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("osInfo")]
    public string OsInfo { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public List<string> Components { get; set; } = new();
}
