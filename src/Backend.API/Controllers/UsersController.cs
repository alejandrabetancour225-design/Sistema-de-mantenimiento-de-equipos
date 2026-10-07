using System.Security.Claims;
using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

[Authorize(Roles = Roles.Administrador)]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetUsers()
    {
        return Ok(await _userService.GetUsersAsync());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(Guid id, UpdateUserRequest request)
    {
        var result = await _userService.UpdateUserAsync(id, request, CurrentUserId());
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<UserResponse>> AssignRole(Guid id, AssignRoleRequest request)
    {
        var result = await _userService.AssignRoleAsync(id, request.Role, CurrentUserId());
        return ToActionResult(result);
    }

    // El Administrador restablece la contraseña de un usuario (flujo de "olvidé mi contraseña").
    // Cierra todas las sesiones de ese usuario y levanta el bloqueo temporal.
    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult<UserResponse>> ResetPassword(Guid id, ResetPasswordRequest request)
    {
        var result = await _userService.ResetPasswordAsync(id, request.NewPassword);
        return ToActionResult(result);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private ActionResult<UserResponse> ToActionResult(UpdateUserResult result)
    {
        return result.Status switch
        {
            UpdateUserStatus.Success => Ok(result.User),
            UpdateUserStatus.UserNotFound => NotFound(new { message = "El usuario no existe." }),
            UpdateUserStatus.RoleNotFound => BadRequest(
                new { message = $"Rol inválido. Roles permitidos: {string.Join(", ", Roles.All)}." }),
            UpdateUserStatus.EmailInUse => Conflict(
                new { message = "El correo ya está registrado por otro usuario." }),
            UpdateUserStatus.LastAdministrator => Conflict(
                new { message = "No se puede desactivar ni quitar el rol al último Administrador activo." }),
            UpdateUserStatus.CannotModifySelf => BadRequest(
                new { message = "No puedes desactivarte ni quitarte el rol de Administrador a ti mismo." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
