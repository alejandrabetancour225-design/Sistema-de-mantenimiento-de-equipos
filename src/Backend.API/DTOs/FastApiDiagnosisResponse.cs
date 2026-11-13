using System.Text.Json.Serialization;

namespace Backend.API.DTOs;

public class FastApiDiagnosisResponse
{
    [JsonPropertyName("ranked_causes")]
    public List<FastApiRankedCause> RankedCauses { get; set; } = new();

    [JsonPropertyName("requires_technician")]
    public bool RequiresTechnician { get; set; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;

    // Modelo que generó el diagnóstico (lo informa el servicio de IA).
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}

public class FastApiRankedCause
{
    [JsonPropertyName("cause")]
    public string Cause { get; set; } = string.Empty;

    [JsonPropertyName("probability")]
    public double Probability { get; set; }

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;
}
