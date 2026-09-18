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
        var result = await _userService.UpdateUserAsync(id, request);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<UserResponse>> AssignRole(Guid id, AssignRoleRequest request)
    {
        var result = await _userService.AssignRoleAsync(id, request.Role);
        return ToActionResult(result);
    }

    private ActionResult<UserResponse> ToActionResult(UpdateUserResult result)
    {
        return result.Status switch
        {
            UpdateUserStatus.Success => Ok(result.User),
            UpdateUserStatus.UserNotFound => NotFound(),
            UpdateUserStatus.RoleNotFound => BadRequest(
                new { message = $"Rol inválido. Roles permitidos: {string.Join(", ", Roles.All)}." }),
            UpdateUserStatus.EmailInUse => Conflict(
                new { message = "El correo ya está registrado por otro usuario." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}