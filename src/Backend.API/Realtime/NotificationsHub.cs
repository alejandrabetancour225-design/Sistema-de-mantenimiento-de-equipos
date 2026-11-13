using Backend.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.API.Realtime;

// Canal en tiempo real (SignalR). Cada conexión entra a dos grupos:
//  - "user:{id}": notificaciones personales (campana).
//  - "org:{id}":  aviso de que cambiaron datos de la organización (dashboard y calendario se refrescan, R3).
[Authorize]
public class NotificationsHub : Hub
{
    public const string Path = "/api/hubs/notificaciones";

    public static string UserGroup(Guid userId) => $"user:{userId}";
    public static string OrgGroup(Guid orgId) => $"org:{orgId}";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User!;
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(user.Id()));
        if (Guid.TryParse(user.FindFirst(AppClaimsOrg.Organization)?.Value, out var org))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, OrgGroup(org));
        }

        await base.OnConnectedAsync();
    }
}
