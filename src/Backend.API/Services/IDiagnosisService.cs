using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IDiagnosisService
{
    Task<DiagnosisResult> DiagnoseAsync(CreateDiagnosisRequest request, Guid currentUserId);
    Task<DiagnosisDetailsResponse?> GetByIdAsync(Guid id);
    Task<List<DiagnosisDetailsResponse>> GetByEquipmentAsync(Guid equipmentId);
    Task<List<DiagnosisDetailsResponse>> GetRecentAsync(int take = 20);
}
