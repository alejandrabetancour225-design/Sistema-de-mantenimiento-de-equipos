using System.Security.Cryptography;
using System.Text;
using Backend.API.Data;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class PasswordResetService : IPasswordResetService
{
    private const int CodeExpiryMinutes = 15;
    private const int MaxFailedAttempts = 5;

    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryption;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        AppDbContext context,
        IEncryptionService encryption,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        ILogger<PasswordResetService> logger)
    {
        _context = context;
        _encryption = encryption;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task RequestResetAsync(string email)
    {

        var emailCandidates = _encryption.ComputeLookupCandidates(email);
        var user = await _context.Users
            .FirstOrDefaultAsync(u => emailCandidates.Contains(u.EmailHash));

        if (user is null || !user.Active)
        {
            _logger.LogInformation("Solicitud de reset para email inexistente o inactivo");
            return;
        }

        var existing = await _context.PasswordResetCodes
            .Where(c => c.UserId == user.Id && c.UsedAt == null)
            .ToListAsync();
        foreach (var c in existing) c.UsedAt = DateTime.UtcNow;

        var code = GenerateCode();
        var codeHash = HashCode(code, user.Id);

        _context.PasswordResetCodes.Add(new PasswordResetCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = codeHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpiryMinutes),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        var decryptedEmail = _encryption.Decrypt(user.Email, EncryptionContexts.UserEmail(user.Id));

        var body = $@"
        Hola {user.FullName},

        Recibimos una solicitud para restablecer tu contraseña.

        Tu código de verificación es: {code}

        Este código expira en {CodeExpiryMinutes} minutos.

        Si no solicitaste este cambio, ignora este mensaje.
        ";
        await _emailSender.SendAsync(decryptedEmail, "Recuperación de contraseña", body);
    }

    public async Task<PasswordResetStatus> ResetAsync(string email, string code, string newPassword)
    {
        var emailCandidates = _encryption.ComputeLookupCandidates(email);
        var user = await _context.Users
            .FirstOrDefaultAsync(u => emailCandidates.Contains(u.EmailHash));

        if (user is null || !user.Active)
            return PasswordResetStatus.InvalidCode;

        var resetCode = await _context.PasswordResetCodes
            .Where(c => c.UserId == user.Id && c.UsedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (resetCode is null || resetCode.ExpiresAt < DateTime.UtcNow)
            return PasswordResetStatus.Expired;

        if (resetCode.FailedAttempts >= MaxFailedAttempts)
            return PasswordResetStatus.TooManyAttempts;

        var expectedHash = HashCode(code, user.Id);
        if (resetCode.CodeHash != expectedHash)
        {
            resetCode.FailedAttempts++;
            await _context.SaveChangesAsync();
            return PasswordResetStatus.InvalidCode;
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.SecurityStamp = Guid.NewGuid();
        user.FailedAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTime.UtcNow;

        resetCode.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Contraseña restablecida para usuario {UserId}", user.Id);
        return PasswordResetStatus.Success;
    }

    private static string GenerateCode()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    private static string HashCode(string code, Guid userId)
    {
        var input = Encoding.UTF8.GetBytes($"{code}:{userId}");
        var hash = SHA256.HashData(input);
        return Convert.ToBase64String(hash);
    }
}
