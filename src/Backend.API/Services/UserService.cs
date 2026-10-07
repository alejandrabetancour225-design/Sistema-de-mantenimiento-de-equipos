using System.Security.Cryptography;
using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class UserService : IUserService
{
    private const string UnreadableValue = "(no se pudo descifrar)";

    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryption;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _audit;
    private readonly ILogger<UserService> _logger;

    public UserService(
        AppDbContext context,
        IEncryptionService encryption,
        IPasswordHasher passwordHasher,
        IAuditService audit,
        ILogger<UserService> logger)
    {
        _context = context;
        _encryption = encryption;
        _passwordHasher = passwordHasher;
        _audit = audit;
        _logger = logger;
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

    public Task<UpdateUserResult> AssignRoleAsync(Guid userId, string role, Guid currentUserId)
    {
        return UpdateUserAsync(userId, new UpdateUserRequest { Role = role }, currentUserId);
    }

    public async Task<UpdateUserResult> UpdateUserAsync(Guid userId, UpdateUserRequest request, Guid currentUserId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return new UpdateUserResult(UpdateUserStatus.UserNotFound, null);
        }

        Role? newRole = null;
        if (request.Role is not null)
        {
            newRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
            if (newRole is null)
            {
                return new UpdateUserResult(UpdateUserStatus.RoleNotFound, null);
            }
        }

        var isAdmin = user.Role?.Name == Roles.Administrador;
        var losesAdminRole = isAdmin && newRole is not null && newRole.Name != Roles.Administrador;
        var getsDeactivated = user.Active && request.Active == false;

        // Un Administrador no puede quitarse el rol ni desactivarse a sí mismo.
        if (userId == currentUserId && (losesAdminRole || getsDeactivated))
        {
            return new UpdateUserResult(UpdateUserStatus.CannotModifySelf, null);
        }

        // Nunca dejar el sistema sin un Administrador activo.
        if (isAdmin && user.Active && (losesAdminRole || getsDeactivated))
        {
            var otherActiveAdmins = await _context.Users.AnyAsync(u =>
                u.Id != userId && u.Active && u.Role != null && u.Role.Name == Roles.Administrador);
            if (!otherActiveAdmins)
            {
                return new UpdateUserResult(UpdateUserStatus.LastAdministrator, null);
            }
        }

        var invalidateSessions = false;

        if (newRole is not null && newRole.Id != user.RoleId)
        {
            user.RoleId = newRole.Id;
            user.Role = newRole;
            invalidateSessions = true;
        }

        if (request.Email is not null)
        {
            var email = _encryption.Normalize(request.Email);
            var emailCandidates = _encryption.ComputeLookupCandidates(email);
            var inUse = await _context.Users.AnyAsync(u => emailCandidates.Contains(u.EmailHash) && u.Id != userId);
            if (inUse)
            {
                return new UpdateUserResult(UpdateUserStatus.EmailInUse, null);
            }

            user.Email = _encryption.Encrypt(email, EncryptionContexts.UserEmail(user.Id));
            user.EmailHash = _encryption.ComputeLookup(email);
            invalidateSessions = true;
        }

        if (request.Phone is not null)
        {
            user.Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? string.Empty
                : _encryption.Encrypt(request.Phone.Trim(), EncryptionContexts.UserPhone(user.Id));
        }

        if (request.Active is not null && request.Active.Value != user.Active)
        {
            user.Active = request.Active.Value;
            invalidateSessions = true;
        }

        // Activar una cuenta también levanta el bloqueo temporal por intentos fallidos.
        if (request.Active == true)
        {
            user.FailedAttempts = 0;
            user.LockoutEnd = null;
        }

        if (invalidateSessions)
        {
            user.SecurityStamp = Guid.NewGuid();
        }

        user.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            return new UpdateUserResult(UpdateUserStatus.EmailInUse, null);
        }

        return new UpdateUserResult(UpdateUserStatus.Success, Map(user));
    }

    public async Task<UpdateUserResult> ResetPasswordAsync(Guid userId, string newPassword)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return new UpdateUserResult(UpdateUserStatus.UserNotFound, null);
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.FailedAttempts = 0;
        user.LockoutEnd = null;
        user.SecurityStamp = Guid.NewGuid();
        user.UpdatedAt = DateTime.UtcNow;

        _audit.Record(AuditActions.PasswordReset, nameof(User), user.Id.ToString());
        await _context.SaveChangesAsync();

        return new UpdateUserResult(UpdateUserStatus.Success, Map(user));
    }

    private UserResponse Map(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = SafeDecrypt(user.Email, EncryptionContexts.UserEmail(user.Id), user.Id) ?? UnreadableValue,
            Phone = string.IsNullOrEmpty(user.Phone)
                ? null
                : SafeDecrypt(user.Phone, EncryptionContexts.UserPhone(user.Id), user.Id) ?? UnreadableValue,
            Active = user.Active,
            Role = user.Role?.Name,
            FailedAttempts = user.FailedAttempts,
            LockoutEnd = user.LockoutEnd,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    // Un registro dañado o cifrado con una clave retirada no debe tumbar todo el listado.
    private string? SafeDecrypt(string ciphertext, string context, Guid userId)
    {
        try
        {
            return _encryption.Decrypt(ciphertext, context);
        }
        catch (CryptographicException)
        {
            _logger.LogError("No se pudo descifrar un dato del usuario {UserId}", userId);
            return null;
        }
    }
}
