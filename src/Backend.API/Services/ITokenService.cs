namespace Backend.API.Services;

public interface ITokenService
{
    string GenerateToken(Guid userId, string email, string? role, Guid securityStamp);
    int ExpiresInMinutes { get; }
}

public static class AppClaims
{
    // Sello de seguridad del usuario: si cambia en la BD, el token deja de ser válido.
    public const string SecurityStamp = "sstamp";
}
