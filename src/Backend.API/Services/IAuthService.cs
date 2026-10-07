using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId);

    // El usuario cambia su propia contraseña. Cierra las demás sesiones y devuelve una sesión nueva.
    Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    // Cambia el sello de seguridad: invalida todos los tokens del usuario.
    Task LogoutAsync(Guid userId);
}
