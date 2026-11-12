using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Backend.API.Infrastructure;

// Registra en AuditLogs cada alta, cambio o borrado que pasa por SaveChanges:
// quién (usuario del token), desde qué IP, sobre qué entidad y qué campos cambiaron.
// Para las entidades operativas (equipos, asignaciones, mantenimientos...) guarda además el valor
// anterior y el nuevo de cada campo, en JSON: {"Status":{"old":"AVAILABLE","new":"IN_USE"}}.
// Las entidades con datos personales o secretos (usuarios, códigos de recuperación) solo registran
// los nombres de los campos, para no duplicar esos datos en la auditoría.
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> EntitiesWithValues = new(StringComparer.Ordinal)
    {
        nameof(Equipment), nameof(EquipmentComponent), nameof(Assignment), nameof(Incident),
        nameof(Maintenance), nameof(MaintenanceSparePart), nameof(SparePart), nameof(ErrorKnowledge), nameof(SlangTerm), nameof(Lessor), nameof(RentalContract), nameof(RentalRate), nameof(LessorClaim), nameof(MaintenancePlan), nameof(ExternalSource)
    };

    private const int MaxValueLength = 300;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // El interceptor corre antes de que EF detecte los cambios.
        context.ChangeTracker.DetectChanges();

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = AuditActor.GetUserId(httpContext);
        var ipAddress = AuditActor.GetIpAddress(httpContext);
        var now = DateTime.UtcNow;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog && e.Entity is not Notification
                && (e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            .ToList();

        foreach (var entry in entries)
        {
            string? details = null;
            if (entry.State == EntityState.Modified)
            {
                var modified = entry.Properties
                    .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)
                        && p.Metadata.Name is not ("FileContent" or "EncryptedConnection" or "PasswordHash"))
                    .ToList();

                if (modified.Count == 0)
                {
                    continue;
                }

                details = EntitiesWithValues.Contains(entry.Metadata.ClrType.Name)
                    ? JsonSerializer.Serialize(modified.ToDictionary(
                        p => p.Metadata.Name,
                        p => new { old = Format(p.OriginalValue), @new = Format(p.CurrentValue) }))
                    : string.Join(",", modified.Select(p => p.Metadata.Name));
            }

            context.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = now,
                UserId = userId,
                Action = entry.State switch
                {
                    EntityState.Added => AuditActions.Create,
                    EntityState.Modified => AuditActions.Update,
                    _ => AuditActions.Delete
                },
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = GetKey(entry),
                OrganizationId = entry.Entity is IOrganizationScoped scoped ? scoped.OrganizationId
                    : entry.Entity is User user ? user.OrganizationId
                    : (context as Backend.API.Data.AppDbContext)?.CurrentOrganizationId is { } org && org != Guid.Empty ? org : null,
                Details = details,
                IpAddress = ipAddress
            });
        }
    }

    private static string? Format(object? value)
    {
        var text = value switch
        {
            null => null,
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

        return text is { Length: > MaxValueLength } ? text[..MaxValueLength] + "…" : text;
    }

    private static string? GetKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return null;
        }

        return string.Join("|", key.Properties.Select(p =>
            Convert.ToString(entry.Property(p.Name).CurrentValue, CultureInfo.InvariantCulture)));
    }
}

public static class AuditActions
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Login = "Login";
    public const string LoginFailed = "LoginFailed";
    public const string LoginLockedOut = "LoginLockedOut";
    public const string Logout = "Logout";
    public const string PasswordReset = "PasswordReset";
    public const string PasswordChanged = "PasswordChanged";
}

public static class AuditActor
{
    public static Guid? GetUserId(HttpContext? httpContext)
    {
        var value = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    public static string? GetIpAddress(HttpContext? httpContext)
        => httpContext?.Connection.RemoteIpAddress?.ToString();
}
