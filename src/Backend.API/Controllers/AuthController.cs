using System.Security.Claims;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
      IAuthService authService,
      IPasswordResetService passwordResetService,
      IConfiguration configuration,
      IWebHostEnvironment environment)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
        _configuration = configuration;
        _environment = environment;
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Register)]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        if (response is null)
        {
            return Conflict(new { message = "No se pudo completar el registro. Si ya tienes una cuenta, inicia sesión." });
        }

        return Ok(WithSession(response));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (response is null)
        {
            // Mensaje único para no revelar si el correo existe, si la cuenta está inactiva o bloqueada.
            return Unauthorized(new { message = "Credenciales inválidas o cuenta no disponible." });
        }

        return Ok(WithSession(response));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var user = await _authService.GetCurrentUserAsync(CurrentUserId());
        return user is null ? Unauthorized() : Ok(user);
    }

    // Cualquier usuario autenticado cambia su propia contraseña.
    // Las demás sesiones se cierran; esta continúa con un token nuevo.
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("change-password")]
    public async Task<ActionResult<AuthResponse>> ChangePassword(ChangePasswordRequest request)
    {
        var result = await _authService.ChangePasswordAsync(CurrentUserId(), request);
        return result.Status switch
        {
            ChangePasswordStatus.Success => Ok(WithSession(result.Session!)),
            ChangePasswordStatus.InvalidCurrentPassword => BadRequest(new { message = "La contraseña actual no es correcta." }),
            ChangePasswordStatus.SameAsCurrent => BadRequest(new { message = "La contraseña nueva debe ser distinta de la actual." }),
            _ => Unauthorized()
        };
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(CurrentUserId());
        AuthCookie.Delete(Response, UseSecureCookie());
        return NoContent();
    }

    private AuthResponse WithSession(AuthResponse response)
    {
        AuthCookie.Append(Response, response.Token!, response.ExpiresAt, UseSecureCookie());

        // Compatibilidad: el frontend actual todavía lee el token del cuerpo.
        // Cuando use la cookie (withCredentials), configurar Auth:ReturnTokenInBody=false.
        if (!_configuration.GetValue("Auth:ReturnTokenInBody", true))
        {
            response.Token = null;
        }

        return response;
    }

    private bool UseSecureCookie() => Request.IsHttps || !_environment.IsDevelopment();

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _passwordResetService.RequestResetAsync(request.Email);
        return Ok(new { message = "Si el correo está registrado, recibirás un código de recuperación." });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var status = await _passwordResetService.ResetAsync(
            request.Email, request.Code, request.NewPassword);

        return status switch
        {
            PasswordResetStatus.Success => Ok(new { message = "Contraseña restablecida correctamente." }),
            PasswordResetStatus.InvalidCode => BadRequest(new { message = "Código inválido." }),
            PasswordResetStatus.Expired => BadRequest(new { message = "El código expiró. Solicita uno nuevo." }),
            PasswordResetStatus.TooManyAttempts => BadRequest(new { message = "Demasiados intentos. Solicita un código nuevo." }),
            _ => BadRequest(new { message = "No se pudo restablecer la contraseña." })
        };
    }
}
