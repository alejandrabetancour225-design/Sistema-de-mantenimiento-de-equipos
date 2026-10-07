using System.Globalization;
using System.Security.Claims;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Backend.API.Infrastructure;

// Registra en AuditLogs cada alta, cambio o borrado que pasa por SaveChanges:
// quién (usuario del token), desde qué IP, sobre qué entidad y qué campos cambiaron.
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
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
            .Where(e => e.Entity is not AuditLog
                && (e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            .ToList();

        foreach (var entry in entries)
        {
            string? details = null;
            if (entry.State == EntityState.Modified)
            {
                details = string.Join(",", entry.Properties
                    .Where(p => p.IsModified)
                    .Select(p => p.Metadata.Name));

                if (details.Length == 0)
                {
                    continue;
                }
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
                Details = details,
                IpAddress = ipAddress
            });
        }
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
