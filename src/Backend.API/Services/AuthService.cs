using System.Security.Cryptography;
using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class AuthService : IAuthService
{
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Hash de relleno: si el correo no existe se verifica igual una contraseña,
    // para que la respuesta tarde lo mismo y no revele qué correos están registrados.
    private static readonly Lazy<string> TimingDummyHash = new(() =>
        new PasswordHasher().Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))));

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEncryptionService _encryption;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEncryptionService encryption,
        IAuditService audit,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _encryption = encryption;
        _audit = audit;
        _logger = logger;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var email = _encryption.Normalize(request.Email);
        var emailCandidates = _encryption.ComputeLookupCandidates(email);
        if (await _context.Users.AnyAsync(u => emailCandidates.Contains(u.EmailHash)))
        {
            return null;
        }

        // El registro público SIEMPRE crea Clientes. El primer Administrador se crea
        // desde la configuración al arrancar (ver AdminBootstrapper).
        var role = await _context.Roles.FirstAsync(r => r.Name == Roles.Cliente);

        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = _encryption.Encrypt(email, EncryptionContexts.UserEmail(userId)),
            EmailHash = _encryption.ComputeLookup(email),
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? string.Empty
                : _encryption.Encrypt(request.Phone.Trim(), EncryptionContexts.UserPhone(userId)),
            Active = true,
            FailedAttempts = 0,
            SecurityStamp = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now,
            RoleId = role.Id,
            Role = role
        };

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // Dos registros simultáneos con el mismo correo.
            return null;
        }

        return BuildAuthResponse(user, email, role.Name);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var emailCandidates = _encryption.ComputeLookupCandidates(request.Email);
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => emailCandidates.Contains(u.EmailHash));

        if (user is null)
        {
            _passwordHasher.Verify(request.Password, TimingDummyHash.Value);
            _logger.LogInformation("Inicio de sesión fallido: correo no registrado");
            return null;
        }

        var now = DateTime.UtcNow;
        var userKey = user.Id.ToString();

        // Bloqueo temporal: no se revisa la contraseña real mientras dure.
        if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
        {
            _passwordHasher.Verify(request.Password, TimingDummyHash.Value);
            _audit.Record(AuditActions.LoginLockedOut, nameof(User), userKey, "Intento durante bloqueo temporal", user.Id);
            await _context.SaveChangesAsync();
            return null;
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= MaxFailedLoginAttempts)
            {
                // Bloqueo temporal para todos los roles (incluido el Administrador).
                // No desactiva la cuenta, así que nadie puede dejar fuera a otro de forma permanente.
                user.LockoutEnd = now.Add(LockoutDuration);
                user.FailedAttempts = 0;
                _audit.Record(AuditActions.LoginLockedOut, nameof(User), userKey,
                    $"Bloqueo temporal de {LockoutDuration.TotalMinutes} minutos", user.Id);
                _logger.LogWarning("Cuenta {UserId} bloqueada temporalmente por intentos fallidos", user.Id);
            }
            else
            {
                _audit.Record(AuditActions.LoginFailed, nameof(User), userKey, "Contraseña incorrecta", user.Id);
            }

            await _context.SaveChangesAsync();
            return null;
        }

        if (!user.Active)
        {
            _audit.Record(AuditActions.LoginFailed, nameof(User), userKey, "Cuenta inactiva", user.Id);
            await _context.SaveChangesAsync();
            return null;
        }

        user.FailedAttempts = 0;
        user.LockoutEnd = null;

        // Hashes antiguos (menos iteraciones) se regeneran con la contraseña recién verificada.
        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = _passwordHasher.Hash(request.Password);
        }

        var emailContext = EncryptionContexts.UserEmail(user.Id);
        var email = _encryption.Normalize(_encryption.Decrypt(user.Email, emailContext));
        UpgradeEncryption(user, email);

        _audit.Record(AuditActions.Login, nameof(User), userKey, userId: user.Id);
        await _context.SaveChangesAsync();

        return BuildAuthResponse(user, email, user.Role?.Name);
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Active);

        if (user is null)
        {
            return null;
        }

        return new CurrentUserResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = _encryption.Decrypt(user.Email, EncryptionContexts.UserEmail(user.Id)),
            Role = user.Role?.Name
        };
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Active);

        if (user is null)
        {
            return new ChangePasswordResult(ChangePasswordStatus.UserNotFound, null);
        }

        var userKey = user.Id.ToString();

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            // Cuenta como intento fallido: protege la cuenta si alguien usa una sesión abierta ajena.
            user.FailedAttempts++;
            if (user.FailedAttempts >= MaxFailedLoginAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedAttempts = 0;
                user.SecurityStamp = Guid.NewGuid();
                _audit.Record(AuditActions.LoginLockedOut, nameof(User), userKey,
                    "Bloqueo por intentos fallidos al cambiar la contraseña", user.Id);
            }
            else
            {
                _audit.Record(AuditActions.LoginFailed, nameof(User), userKey,
                    "Contraseña actual incorrecta al cambiar la contraseña", user.Id);
            }

            await _context.SaveChangesAsync();
            return new ChangePasswordResult(ChangePasswordStatus.InvalidCurrentPassword, null);
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            return new ChangePasswordResult(ChangePasswordStatus.SameAsCurrent, null);
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.FailedAttempts = 0;
        user.LockoutEnd = null;
        user.SecurityStamp = Guid.NewGuid();
        user.UpdatedAt = DateTime.UtcNow;

        _audit.Record(AuditActions.PasswordChanged, nameof(User), userKey, userId: user.Id);
        await _context.SaveChangesAsync();

        var email = _encryption.Normalize(_encryption.Decrypt(user.Email, EncryptionContexts.UserEmail(user.Id)));
        return new ChangePasswordResult(ChangePasswordStatus.Success, BuildAuthResponse(user, email, user.Role?.Name));
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return;
        }

        user.SecurityStamp = Guid.NewGuid();
        _audit.Record(AuditActions.Logout, nameof(User), user.Id.ToString(), userId: user.Id);
        await _context.SaveChangesAsync();
    }

    // Migra correo y teléfono al formato de cifrado actual (v2, ligado al usuario y la clave vigente).
    private void UpgradeEncryption(User user, string normalizedEmail)
    {
        var emailContext = EncryptionContexts.UserEmail(user.Id);
        if (!_encryption.IsCurrentFormat(user.Email, emailContext))
        {
            user.Email = _encryption.Encrypt(normalizedEmail, emailContext);
            user.EmailHash = _encryption.ComputeLookup(normalizedEmail);
        }

        var phoneContext = EncryptionContexts.UserPhone(user.Id);
        if (!string.IsNullOrEmpty(user.Phone) && !_encryption.IsCurrentFormat(user.Phone, phoneContext))
        {
            user.Phone = _encryption.Encrypt(_encryption.Decrypt(user.Phone, phoneContext), phoneContext);
        }
    }

    private AuthResponse BuildAuthResponse(User user, string email, string? role)
    {
        return new AuthResponse
        {
            Token = _tokenService.GenerateToken(user.Id, email, role, user.SecurityStamp),
            UserId = user.Id,
            FullName = user.FullName,
            Email = email,
            Role = role,
            Active = user.Active,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_tokenService.ExpiresInMinutes)
        };
    }
}
