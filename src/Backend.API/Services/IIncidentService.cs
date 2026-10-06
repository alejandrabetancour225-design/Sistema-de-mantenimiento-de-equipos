using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IIncidentService
{
    Task<List<IncidentResponse>> GetAllAsync();
    Task<IncidentResponse?> GetByIdAsync(Guid id);
    Task<List<IncidentResponse>> GetByEquipmentAsync(Guid equipmentId);
    Task<IncidentResult> CreateAsync(CreateIncidentRequest request, Guid reportedBy);
    Task<IncidentResult> UpdateAsync(Guid id, UpdateIncidentRequest request, Guid currentUserId, string? currentRole);
    Task<IncidentActionStatus> DeleteAsync(Guid id, Guid currentUserId, string? currentRole);
}