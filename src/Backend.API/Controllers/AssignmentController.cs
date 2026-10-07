using System.Security.Claims;
using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

// Cliente: sin acceso. Empleado: solo consulta sus propias asignaciones.
[Authorize(Roles = Roles.Staff)]
[ApiController]
[Route("api/[controller]")]
public class AssignmentController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;

    public AssignmentController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
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
            AssignmentActionStatus.EquipmentNotAssignable => BadRequest(new { message = "El equipo está en mantenimiento, fuera de servicio o dado de baja; no se puede asignar." }),
            AssignmentActionStatus.UserInactive => BadRequest(new { message = "El usuario está inactivo." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}