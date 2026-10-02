using Backend.API.DTOs;

namespace Backend.API.Services;

public interface ISparePartService
{
    Task<List<SparePartResponse>> GetAllAsync(bool includeInactive = false);
    Task<SparePartResponse?> GetByIdAsync(Guid id);
    Task<SparePartResult> CreateAsync(CreateSparePartRequest request);
    Task<SparePartResult> UpdateAsync(Guid id, UpdateSparePartRequest request);
    Task<SparePartActionStatus> DeactivateAsync(Guid id);
    Task<SparePartActionStatus> ReactivateAsync(Guid id);
}
