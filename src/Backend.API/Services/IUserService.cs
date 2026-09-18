using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IUserService
{
    Task<List<UserResponse>> GetUsersAsync();
    Task<UpdateUserResult> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<UpdateUserResult> AssignRoleAsync(Guid userId, string role);
}