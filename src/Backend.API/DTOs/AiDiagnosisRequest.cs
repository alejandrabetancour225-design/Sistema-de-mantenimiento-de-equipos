using System.Text.Json.Serialization;

namespace Backend.API.DTOs;

public class AiDiagnosisRequest
{
    [JsonPropertyName("equipment_id")]
    public Guid EquipmentId { get; set; }

    [JsonPropertyName("symptom")]
    public string Symptom { get; set; } = string.Empty;

    [JsonPropertyName("equipment_context")]
    public AiEquipmentContext EquipmentContext { get; set; } = new();

    [JsonPropertyName("knowledge_context")]
    public List<AiKnowledgeItem> KnowledgeContext { get; set; } = new();

    // Glosario de modismos: el servicio de IA detecta las expresiones y las explica en el prompt.
    [JsonPropertyName("glossary")]
    public List<AiGlossaryItem> Glossary { get; set; } = new();
}

public class AiGlossaryItem
{
    [JsonPropertyName("expresion")]
    public string Expresion { get; set; } = string.Empty;

    [JsonPropertyName("significado")]
    public string Significado { get; set; } = string.Empty;

    [JsonPropertyName("categoria")]
    public string? Categoria { get; set; }
}

public class AiEquipmentContext
{
    [JsonPropertyName("brand")]
    public string Brand { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("os_info")]
    public string? OsInfo { get; set; }

    [JsonPropertyName("components")]
    public List<string> Components { get; set; } = new();
}

public class AiKnowledgeItem
{
    [JsonPropertyName("symptom")]
    public string Symptom { get; set; } = string.Empty;

    [JsonPropertyName("possibleCauses")]
    public string PossibleCauses { get; set; } = string.Empty;

    [JsonPropertyName("suggestedSolution")]
    public string SuggestedSolution { get; set; } = string.Empty;
}
