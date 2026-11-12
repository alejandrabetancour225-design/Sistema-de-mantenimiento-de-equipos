using Backend.API.DTOs;
using Backend.API.Models;

namespace Backend.API.Services;

public interface IEquipmentService
{
    Task<List<EquipmentResponse>> GetAllAsync(Guid currentUserId, string? role);
    Task<EquipmentResponse?> GetByIdAsync(Guid id, Guid currentUserId, string? role);
    Task<bool> CanViewAsync(Guid equipmentId, Guid currentUserId, string? role);
    Task<EquipmentResult> CreateAsync(CreateEquipmentRequest request);
    Task<EquipmentResult> UpdateAsync(Guid id, UpdateEquipmentRequest request, bool isAdmin);
    Task<EquipmentResult> ChangeStatusAsync(Guid id, EquipmentStatus status);
    Task<EquipmentActionStatus> DeleteAsync(Guid id);
}
