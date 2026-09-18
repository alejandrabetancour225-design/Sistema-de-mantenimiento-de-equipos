using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedLoginAttempts = 3;

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEncryptionService _encryption;

    public AuthService(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEncryptionService encryption)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _encryption = encryption;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var emailHash = _encryption.ComputeLookup(request.Email);
        var emailExists = await _context.Users.AnyAsync(u => u.EmailHash == emailHash);
        if (emailExists)
        {
            return null;
        }

        var adminExists = await _context.Users.AnyAsync(u => u.Role != null && u.Role.Name == Roles.Administrador);
        var role = await _context.Roles.FirstAsync(r => r.Name == (adminExists ? Roles.Cliente : Roles.Administrador));

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = _encryption.Encrypt(_encryption.Normalize(request.Email)),
            EmailHash = emailHash,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? string.Empty : _encryption.Encrypt(request.Phone.Trim()),
            Active = true,
            FailedAttempts = 0,
            CreatedAt = now,
            UpdatedAt = now,
            RoleId = role.Id,
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return BuildAuthResponse(user.Id, _encryption.Normalize(request.Email), user.FullName, role.Name, user.Active);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var emailHash = _encryption.ComputeLookup(request.Email);
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.EmailHash == emailHash);

        if (user is null)
        {
            return null;
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedAttempts++;
            if (user.FailedAttempts > MaxFailedLoginAttempts)
            {
                user.Active = false;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return null;
        }

        if (!user.Active)
        {
            return null;
        }

        if (user.FailedAttempts != 0)
        {
            user.FailedAttempts = 0;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        var email = _encryption.Decrypt(user.Email);
        return BuildAuthResponse(user.Id, email, user.FullName, user.Role?.Name, user.Active);
    }

    private AuthResponse BuildAuthResponse(Guid userId, string email, string fullName, string? role, bool active)
    {
        return new AuthResponse
        {
            Token = _tokenService.GenerateToken(userId, email, role),
            FullName = fullName,
            Email = email,
            Role = role,
            Active = active
        };
    }
}