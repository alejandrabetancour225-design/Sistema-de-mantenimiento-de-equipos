using System.Security.Claims;

namespace Backend.API.Infrastructure;

public static class CurrentUser
{
    public static Guid Id(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string? Role(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Role);
}
