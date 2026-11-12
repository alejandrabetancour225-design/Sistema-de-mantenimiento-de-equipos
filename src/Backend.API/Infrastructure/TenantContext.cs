using System.Security.Claims;
using Backend.API.Models;

namespace Backend.API.Infrastructure;

public static class AppClaimsOrg
{
    public const string Organization = "org";
}

// Organización de la petición actual, tomada del token.
//  - Usuario de una empresa  -> OrganizationId = su empresa (solo ve esos datos).
//  - SuperAdmin              -> OrganizationId = Guid.Empty (no ve datos de ninguna empresa).
//  - Sin usuario (login, arranque, tareas programadas) -> null: sin filtro; el código
//    que corre así debe indicar la organización explícitamente.
public interface ITenantContext
{
    Guid? OrganizationId { get; }
    bool IsSuperAdmin { get; }
}

public sealed class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpTenantContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public bool IsSuperAdmin => User?.IsInRole(Roles.SuperAdmin) == true;

    public Guid? OrganizationId
    {
        get
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            if (IsSuperAdmin)
            {
                return Guid.Empty;
            }

            return Guid.TryParse(User.FindFirstValue(AppClaimsOrg.Organization), out var org) ? org : Guid.Empty;
        }
    }
}
