namespace Backend.API.DTOs;

public enum FeedbackActionStatus
{
    Success,
    DiagnosisNotFound,
    FeedbackAlreadyExists,
    TechnicianNotFound,
    DiagnosisNotPending
}

public record FeedbackResult(FeedbackActionStatus Status, FeedbackResponse? Feedback);
