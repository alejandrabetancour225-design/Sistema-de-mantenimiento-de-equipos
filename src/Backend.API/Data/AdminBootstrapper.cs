using System.ComponentModel.DataAnnotations;
using Backend.API.Models;
using Backend.API.Services;
using Backend.API.Validation;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Data;

// Crea el primer Administrador a partir de la configuración (user-secrets o variables de entorno),
// solo si no existe ningún Administrador activo. El registro público nunca crea Administradores.
//   Bootstrap:AdminEmail, Bootstrap:AdminPassword, Bootstrap:AdminFullName (opcional)
public static class AdminBootstrapper
{
    public static async Task EnsureAdministratorAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<AppDbContext>();

        bool adminExists;
        try
        {
            adminExists = await context.Users.AnyAsync(
                u => u.Active && u.Role != null && u.Role.Name == Roles.Administrador,
                cancellationToken);
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or InvalidOperationException)
        {
            logger.LogWarning(ex,
                "No se pudo verificar si existe un Administrador. ¿Están aplicadas las migraciones?");
            return;
        }

        if (adminExists)
        {
            return;
        }

        var email = configuration["Bootstrap:AdminEmail"];
        var password = configuration["Bootstrap:AdminPassword"];
        var fullName = configuration["Bootstrap:AdminFullName"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No hay ningún Administrador activo. Define Bootstrap:AdminEmail y Bootstrap:AdminPassword "
                + "(user-secrets o variables de entorno) y reinicia la API para crearlo.");
            return;
        }

        if (!new EmailAddressAttribute().IsValid(email))
        {
            logger.LogError("Bootstrap:AdminEmail no es un correo válido. No se creó el Administrador.");
            return;
        }

        if (!StrongPasswordAttribute.IsStrong(password))
        {
            logger.LogError(
                "Bootstrap:AdminPassword no cumple la política (mínimo {Min} caracteres, letras y números). No se creó el Administrador.",
                StrongPasswordAttribute.MinLength);
            return;
        }

        var encryption = services.GetRequiredService<IEncryptionService>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();

        var normalizedEmail = encryption.Normalize(email);
        var candidates = encryption.ComputeLookupCandidates(normalizedEmail);
        if (await context.Users.AnyAsync(u => candidates.Contains(u.EmailHash), cancellationToken))
        {
            // Si alguien ya se registró con ese correo, no se le da el rol automáticamente:
            // podría ser un tercero que se adelantó a registrarlo.
            logger.LogError(
                "El correo de Bootstrap:AdminEmail ya pertenece a un usuario existente. Por seguridad no se le asigna "
                + "el rol Administrador automáticamente. Usa otro correo.");
            return;
        }

        var role = await context.Roles.FirstAsync(r => r.Name == Roles.Administrador, cancellationToken);
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        context.Users.Add(new User
        {
            Id = userId,
            Email = encryption.Encrypt(normalizedEmail, EncryptionContexts.UserEmail(userId)),
            EmailHash = encryption.ComputeLookup(normalizedEmail),
            PasswordHash = passwordHasher.Hash(password),
            FullName = string.IsNullOrWhiteSpace(fullName) ? "Administrador" : fullName.Trim(),
            Phone = string.Empty,
            Active = true,
            FailedAttempts = 0,
            SecurityStamp = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now,
            RoleId = role.Id
        });

        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Se creó el Administrador inicial ({UserId}). Ya puedes quitar Bootstrap:AdminPassword de la configuración.",
            userId);
    }
}
