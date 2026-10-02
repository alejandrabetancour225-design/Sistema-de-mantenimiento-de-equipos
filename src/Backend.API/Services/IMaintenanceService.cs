using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IMaintenanceService
{
    Task<List<MaintenanceResponse>> GetAllAsync();
    Task<MaintenanceResponse?> GetByIdAsync(Guid id);
    Task<List<MaintenanceResponse>> GetByEquipmentAsync(Guid equipmentId);
    Task<MaintenanceResult> CreateAsync(CreateMaintenanceRequest request);
    Task<MaintenanceResult> CreateFromIncidentAsync(Guid incidentId, Guid currentUserId);
    Task<MaintenanceResult> UpdateAsync(Guid id, UpdateMaintenanceRequest request);
    Task<MaintenanceResult> CloseAsync(Guid id, CloseMaintenanceRequest request);
    Task<MaintenanceResult> AddSparePartAsync(Guid maintenanceId, AddSparePartToMaintenanceRequest request);
    Task<MaintenanceActionStatus> RemoveSparePartAsync(Guid maintenanceId, Guid maintenanceSparePartId);
    Task<MaintenanceActionStatus> DeleteAsync(Guid id);
    Task<EquipmentHistoryResponse?> GetHistoryByEquipmentAsync(Guid equipmentId);
}
