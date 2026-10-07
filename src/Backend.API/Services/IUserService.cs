using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IUserService
{
    Task<List<UserResponse>> GetUsersAsync();
    Task<UpdateUserResult> UpdateUserAsync(Guid userId, UpdateUserRequest request, Guid currentUserId);
    Task<UpdateUserResult> AssignRoleAsync(Guid userId, string role, Guid currentUserId);
    Task<UpdateUserResult> ResetPasswordAsync(Guid userId, string newPassword);
}
