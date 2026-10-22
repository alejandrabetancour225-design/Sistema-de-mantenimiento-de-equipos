using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IPredictionService
{
    Task<List<FailurePredictionResponse>> GetHighRiskAsync(int take = 10);
    Task<FailurePredictionResponse?> GetByEquipmentAsync(Guid equipmentId);
    Task<PredictionResult> RefreshForEquipmentAsync(Guid equipmentId);
    Task<int> RefreshAllAsync();
    Task<bool> AcknowledgeAsync(Guid predictionId);
}
