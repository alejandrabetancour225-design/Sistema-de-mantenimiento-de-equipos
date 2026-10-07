namespace Backend.API.DTOs;

public class AuthResponse
{
    // El token también se entrega en una cookie httpOnly. Se sigue devolviendo en el cuerpo
    // mientras el frontend no migre a la cookie (ver Auth:ReturnTokenInBody).
    public string? Token { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
    public bool Active { get; set; }
    public DateTime ExpiresAt { get; set; }
}
