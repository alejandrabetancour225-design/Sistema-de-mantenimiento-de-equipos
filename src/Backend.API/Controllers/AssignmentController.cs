using System.Security.Claims;
using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

// Cliente: sin acceso. Empleado: solo consulta sus propias asignaciones.
[Authorize(Roles = Roles.Staff)]
[ApiController]
[Route("api/[controller]")]
public class AssignmentController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly AppDbContext _context;

    public AssignmentController(IAssignmentService assignmentService, AppDbContext context)
    {
        _assignmentService = assignmentService;
        _context = context;
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("AssignEquipment")]
    public async Task<ActionResult<AssignmentResponse>> AssignEquipment(CreateAssignmentRequest request)
    {
        var result = await _assignmentService.AssignAsync(request);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPatch("ReleaseEquipment/{id:guid}")]
    public async Task<ActionResult<AssignmentResponse>> ReleaseEquipment(Guid id, ReleaseAssignmentRequest? request)
    {
        var result = await _assignmentService.ReleaseAsync(id, request?.Observations);
        return ToActionResult(result);
    }

    // Personas a las que se puede asignar un equipo: personal interno activo de la organización.
    // (Antes el Técnico solo veía a quienes ya tenían alguna asignación; un empleado nuevo no aparecía.)
    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpGet("GetAssignableUsers")]
    public async Task<ActionResult<object>> GetAssignableUsers()
        => Ok(await _context.Users.AsNoTracking()
            .Where(u => u.Active && u.Role != null
                && (u.Role.Name == Roles.Empleado || u.Role.Name == Roles.Tecnico || u.Role.Name == Roles.Administrador))
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName, Role = u.Role!.Name })
            .ToListAsync());

    [HttpGet("GetAssignments")]
    public async Task<ActionResult<List<AssignmentResponse>>> GetAssignments()
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);
        return Ok(await _assignmentService.GetAllAsync(currentUserId, role));
    }

    private ActionResult<AssignmentResponse> ToActionResult(AssignmentResult result)
    {
        return result.Status switch
        {
            AssignmentActionStatus.Success => Ok(result.Assignment),
            AssignmentActionStatus.NotFound => NotFound(new { message = "La asignación no existe." }),
            AssignmentActionStatus.EquipmentNotFound => NotFound(new { message = "El equipo no existe." }),
            AssignmentActionStatus.UserNotFound => NotFound(new { message = "El usuario no existe." }),
            AssignmentActionStatus.EquipmentAlreadyAssigned => Conflict(new { message = "El equipo ya está asignado (tiene una asignación activa)." }),
            AssignmentActionStatus.NotActive => BadRequest(new { message = "Solo se pueden liberar asignaciones activas." }),
            AssignmentActionStatus.EquipmentNotAssignable => BadRequest(new { message = "Solo se puede asignar un equipo Disponible (no en mantenimiento, fuera de servicio ni dado de baja)." }),
            AssignmentActionStatus.UserNotAssignable => BadRequest(new { message = "Los equipos solo se asignan a personal interno (Empleado, Técnico o Administrador)." }),
            AssignmentActionStatus.UserInactive => BadRequest(new { message = "El usuario está inactivo." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}