namespace Backend.API.DTOs;

public enum DiagnosisActionStatus
{
    Success,
    EquipmentNotFound,
    IncidentNotFound,
    IncidentNotOpen,
    AiServiceUnavailable,
    AiResponseInvalid
}

public record DiagnosisResult(DiagnosisActionStatus Status, DiagnosisDetailsResponse? Diagnosis);
