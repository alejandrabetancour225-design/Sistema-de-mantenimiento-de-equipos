using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryption;

    public UserService(AppDbContext context, IEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<List<UserResponse>> GetUsersAsync()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return users.Select(Map).ToList();
    }

    public Task<UpdateUserResult> AssignRoleAsync(Guid userId, string role)
    {
        return UpdateUserAsync(userId, new UpdateUserRequest { Role = role });
    }

    public async Task<UpdateUserResult> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return new UpdateUserResult(UpdateUserStatus.UserNotFound, null);
        }

        if (request.Role is not null)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
            if (role is null)
            {
                return new UpdateUserResult(UpdateUserStatus.RoleNotFound, null);
            }

            user.RoleId = role.Id;
            user.Role = role;
        }

        if (request.Email is not null)
        {
            var emailHash = _encryption.ComputeLookup(request.Email);
            var inUse = await _context.Users.AnyAsync(u => u.EmailHash == emailHash && u.Id != userId);
            if (inUse)
            {
                return new UpdateUserResult(UpdateUserStatus.EmailInUse, null);
            }

            user.Email = _encryption.Encrypt(_encryption.Normalize(request.Email));
            user.EmailHash = emailHash;
        }

        if (request.Phone is not null)
        {
            user.Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? string.Empty
                : _encryption.Encrypt(request.Phone.Trim());
        }

        if (request.Active is not null)
        {
            user.Active = request.Active.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new UpdateUserResult(UpdateUserStatus.Success, Map(user));
    }

    private UserResponse Map(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = _encryption.Decrypt(user.Email),
            Phone = string.IsNullOrEmpty(user.Phone) ? null : _encryption.Decrypt(user.Phone),
            Active = user.Active,
            Role = user.Role?.Name,
            FailedAttempts = user.FailedAttempts,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}