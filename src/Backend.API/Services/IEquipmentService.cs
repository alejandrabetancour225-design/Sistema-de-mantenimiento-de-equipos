using Backend.API.DTOs;
using Backend.API.Models;

namespace Backend.API.Services;

public interface IEquipmentService
{
    Task<List<EquipmentResponse>> GetAllAsync();
    Task<EquipmentResponse?> GetByIdAsync(Guid id);
    Task<EquipmentResult> CreateAsync(CreateEquipmentRequest request);
    Task<EquipmentResult> UpdateAsync(Guid id, UpdateEquipmentRequest request);
    Task<EquipmentResult> ChangeStatusAsync(Guid id, EquipmentStatus status);
    Task<EquipmentActionStatus> DeleteAsync(Guid id);
}