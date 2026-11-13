using Backend.API.Data;
using Backend.API.Models;
using Backend.API.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public record NotificationRequest(
    Guid OrganizationId,
    IReadOnlyCollection<Guid> UserIds,
    string Title,
    string Body,
    string? Link,
    string Type,
    string? DedupKey = null,
    bool SendEmail = false);

public interface INotificationService
{
    // Crea la notificación para cada usuario (sin repetir la misma DedupKey), la envía por
    // SignalR a la campana y, si se pide, por correo. Devuelve cuántas se crearon.
    Task<int> NotifyAsync(NotificationRequest request, CancellationToken ct = default);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly IEmailSender _email;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, IHubContext<NotificationsHub> hub, IEmailSender email,
        IEncryptionService encryption, ILogger<NotificationService> logger)
    {
        _context = context;
        _hub = hub;
        _email = email;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<int> NotifyAsync(NotificationRequest request, CancellationToken ct = default)
    {
        var userIds = request.UserIds.Distinct().ToList();
        if (userIds.Count == 0)
        {
            return 0;
        }

        var alreadySent = request.DedupKey is null
            ? new HashSet<Guid>()
            : (await _context.Notifications.IgnoreQueryFilters()
                .Where(n => n.DedupKey == request.DedupKey && userIds.Contains(n.UserId))
                .Select(n => n.UserId).ToListAsync(ct)).ToHashSet();

        var now = DateTime.UtcNow;
        var created = userIds.Where(id => !alreadySent.Contains(id)).Select(id => new Notification
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            UserId = id,
            Title = request.Title,
            Body = request.Body,
            Link = request.Link,
            Type = request.Type,
            DedupKey = request.DedupKey,
            CreatedAt = now
        }).ToList();

        if (created.Count == 0)
        {
            return 0;
        }

        _context.Notifications.AddRange(created);
        await _context.SaveChangesAsync(ct);

        foreach (var n in created)
        {
            await _hub.Clients.Group(NotificationsHub.UserGroup(n.UserId)).SendAsync("notificacion",
                new { n.Id, n.Title, n.Body, n.Link, n.Type, n.CreatedAt }, ct);
        }

        if (request.SendEmail)
        {
            var users = await _context.Users.IgnoreQueryFilters().AsNoTracking()
                .Where(u => created.Select(c => c.UserId).Contains(u.Id) && u.Active)
                .Select(u => new { u.Id, u.Email }).ToListAsync(ct);
            foreach (var user in users)
            {
                try
                {
                    var to = _encryption.Decrypt(user.Email, EncryptionContexts.UserEmail(user.Id));
                    await _email.SendAsync(to, request.Title, $"{request.Body}\n\nSistema de Mantenimiento de Equipos");
                }
                catch (Exception ex)
                {
                    // El aviso en la app ya quedó; un correo fallido no detiene los demás.
                    _logger.LogWarning(ex, "No se pudo enviar el correo de notificación al usuario {UserId}", user.Id);
                }
            }
        }

        return created.Count;
    }
}
