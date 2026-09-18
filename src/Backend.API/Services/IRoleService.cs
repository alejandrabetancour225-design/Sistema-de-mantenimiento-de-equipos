using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IRoleService
{
    Task<List<RoleResponse>> GetRolesAsync();
}