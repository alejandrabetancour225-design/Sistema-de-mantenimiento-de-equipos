using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.API.DTOs;

// ---------- Contrato con python-service (/api/v1/triage) ----------
public class AiTriageRequest
{
    [JsonPropertyName("equipment_context")]
    public AiEquipmentContext EquipmentContext { get; set; } = new();

    [JsonPropertyName("messages")]
    public List<AiChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("questions_asked")]
    public int QuestionsAsked { get; set; }

    [JsonPropertyName("max_questions")]
    public int MaxQuestions { get; set; } = 3;

    [JsonPropertyName("glossary")]
    public List<AiGlossaryItem> Glossary { get; set; } = new();
}

public class AiChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "empleado";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class AiTriageResponse
{
    [JsonPropertyName("understanding")]
    public string Understanding { get; set; } = string.Empty;

    [JsonPropertyName("needs_more_info")]
    public bool NeedsMoreInfo { get; set; }

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("safe_tips")]
    public List<string> SafeTips { get; set; } = new();

    [JsonPropertyName("technical_summary")]
    public string TechnicalSummary { get; set; } = string.Empty;

    [JsonPropertyName("urgency")]
    public string Urgency { get; set; } = "media";

    [JsonPropertyName("requires_technician")]
    public bool RequiresTechnician { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("detected_expressions")]
    public List<string> DetectedExpressions { get; set; } = new();
}

// ---------- Contrato con el frontend (/api/assistant/triage) ----------
public class TriageChatMessage
{
    // "empleado" o "asistente"
    [Required]
    [RegularExpression("^(empleado|asistente)$", ErrorMessage = "Rol de mensaje inválido.")]
    public string Role { get; set; } = "empleado";

    [Required]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Cada mensaje debe tener entre 1 y 1000 caracteres.")]
    public string Content { get; set; } = string.Empty;
}

public class TriageChatRequest
{
    [Required]
    public Guid EquipmentId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(8, ErrorMessage = "La conversación es demasiado larga.")]
    public List<TriageChatMessage> Messages { get; set; } = new();
}
