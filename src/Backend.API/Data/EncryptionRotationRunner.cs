using System.Security.Cryptography;
using System.Text.Json;
using Backend.API.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Data;

public sealed record EncryptionRotationReport(
    int Scanned,
    int Rotated,
    int AlreadyCurrent,
    int Failed,
    int Verified,
    string BackupPath);

public sealed record RotationBackupRow(Guid Id, string Email, string EmailHash, string Phone);

public static class EncryptionRotationRunner
{
    private const int BatchSize = 50;

    public static async Task<EncryptionRotationReport> RunAsync(
        AppDbContext context,
        IEncryptionService encryption,
        ILogger logger,
        string backupDirectory,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new RotationBackupRow(u.Id, u.Email, u.EmailHash, u.Phone))
            .ToListAsync(cancellationToken);

        var backupPath = WriteBackup(rows, backupDirectory);
        logger.LogWarning("Respaldo previo a la rotación escrito en {BackupPath}", backupPath);

        var rotated = 0;
        var alreadyCurrent = 0;
        var failed = 0;
        var batchNumber = 0;

        for (var offset = 0; offset < rows.Count; offset += BatchSize)
        {
            batchNumber++;
            var batch = rows.Skip(offset).Take(BatchSize).ToList();
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var changed = false;

            foreach (var row in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var user = await context.Users.FirstAsync(u => u.Id == row.Id, cancellationToken);

                string email;
                string phone;
                try
                {
                    email = encryption.Normalize(encryption.Decrypt(user.Email));
                    phone = string.IsNullOrEmpty(user.Phone) ? string.Empty : encryption.Decrypt(user.Phone);
                }
                catch (CryptographicException ex)
                {
                    failed++;
                    logger.LogError("No se pudo descifrar el usuario {UserId}: {Reason}", user.Id, ex.Message);
                    continue;
                }

                var newHash = encryption.ComputeLookup(email);
                if (user.EmailHash == newHash)
                {
                    alreadyCurrent++;
                    continue;
                }

                user.Email = encryption.Encrypt(email);
                user.EmailHash = newHash;
                user.Phone = phone.Length == 0 ? string.Empty : encryption.Encrypt(phone);
                user.UpdatedAt = DateTime.UtcNow;
                changed = true;
                rotated++;
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation(
                "Lote {Batch}: revisados={Scanned}, rotados={Rotated}, ya vigentes={Current}, fallidos={Failed}",
                batchNumber, batch.Count, rotated, alreadyCurrent, failed);
        }

        var verified = await VerifyAsync(context, encryption, logger, cancellationToken);

        return new EncryptionRotationReport(rows.Count, rotated, alreadyCurrent, failed, verified, backupPath);
    }

    public static async Task<int> VerifyAsync(
        AppDbContext context,
        IEncryptionService encryption,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var users = await context.Users.AsNoTracking().ToListAsync(cancellationToken);
        var verified = 0;

        foreach (var user in users)
        {
            if (!encryption.TryDecryptWithPrimaryKey(user.Email, out var email))
            {
                logger.LogError("Verificación: el correo de {UserId} no se descifra con Encryption:Key", user.Id);
                continue;
            }

            if (!string.Equals(email, encryption.Normalize(email), StringComparison.Ordinal))
            {
                logger.LogError("Verificación: el correo de {UserId} no está normalizado", user.Id);
                continue;
            }

            if (!string.Equals(user.EmailHash, encryption.ComputeLookup(email), StringComparison.Ordinal))
            {
                logger.LogError("Verificación: EmailHash de {UserId} no corresponde a Encryption:Key", user.Id);
                continue;
            }

            if (!string.IsNullOrEmpty(user.Phone) && !encryption.TryDecryptWithPrimaryKey(user.Phone, out _))
            {
                logger.LogError("Verificación: el teléfono de {UserId} no se descifra con Encryption:Key", user.Id);
                continue;
            }

            verified++;
        }

        return verified;
    }

    private static string WriteBackup(IReadOnlyList<RotationBackupRow> rows, string backupDirectory)
    {
        Directory.CreateDirectory(backupDirectory);
        var path = Path.Combine(
            backupDirectory,
            $"encryption-rotation-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(
            path,
            JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));

        return path;
    }
}
