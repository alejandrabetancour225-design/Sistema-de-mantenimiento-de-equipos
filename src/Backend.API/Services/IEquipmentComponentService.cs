using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IEquipmentComponentService
{
    Task<List<EquipmentComponentResponse>> GetAllAsync();
    Task<EquipmentComponentResponse?> GetByIdAsync(Guid id);
    Task<List<EquipmentComponentResponse>> GetByEquipmentAsync(Guid equipmentId);
    Task<EquipmentComponentResult> CreateAsync(CreateEquipmentComponentRequest request);
    Task<EquipmentComponentResult> UpdateAsync(Guid id, UpdateEquipmentComponentRequest request);
    Task<EquipmentComponentActionStatus> DeleteAsync(Guid id);
}