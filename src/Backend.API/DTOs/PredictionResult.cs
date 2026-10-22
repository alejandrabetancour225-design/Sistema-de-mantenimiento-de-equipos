namespace Backend.API.DTOs;

public enum PredictionActionStatus
{
    Success,
    EquipmentNotFound,
    NotFound
}

public record PredictionResult(PredictionActionStatus Status, FailurePredictionResponse? Prediction);
