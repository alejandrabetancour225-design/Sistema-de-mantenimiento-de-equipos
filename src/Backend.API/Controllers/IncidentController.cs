using System.Security.Claims;
using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IncidentController : ControllerBase
{
    private const string AdminOrEmployee = Roles.Administrador + "," + Roles.Empleado;

    private readonly IIncidentService _incidentService;

    public IncidentController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [HttpGet("GetIncidentList")]
    public async Task<ActionResult<List<IncidentResponse>>> GetIncidentList()
    {
        return Ok(await _incidentService.GetAllAsync());
    }

    [HttpGet("GetIncidentById/{id:guid}")]
    public async Task<ActionResult<IncidentResponse>> GetIncidentById(Guid id)
    {
        var incident = await _incidentService.GetByIdAsync(id);
        return incident is null ? NotFound() : Ok(incident);
    }

    [HttpGet("GetIncidentsByEquipment/{equipmentId:guid}")]
    public async Task<ActionResult<List<IncidentResponse>>> GetIncidentsByEquipment(Guid equipmentId)
    {
        return Ok(await _incidentService.GetByEquipmentAsync(equipmentId));
    }

    [Authorize(Roles = AdminOrEmployee)]
    [HttpPost("PostNewIncident")]
    public async Task<ActionResult<IncidentResponse>> PostNewIncident(CreateIncidentRequest request)
    {
        var reportedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _incidentService.CreateAsync(request, reportedBy);
        return ToActionResult(result);
    }

    [Authorize(Roles = AdminOrEmployee)]
    [HttpPut("PutIncident/{id:guid}")]
    public async Task<ActionResult<IncidentResponse>> PutIncident(Guid id, UpdateIncidentRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        var result = await _incidentService.UpdateAsync(id, request, currentUserId, currentRole);
        return ToActionResult(result);
    }

    [Authorize(Roles = AdminOrEmployee)]
    [HttpDelete("DeleteIncident/{id:guid}")]
    public async Task<IActionResult> DeleteIncident(Guid id)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        var status = await _incidentService.DeleteAsync(id, currentUserId, currentRole);
        return status == IncidentActionStatus.NotFound ? NotFound() : NoContent();
    }

    private ActionResult<IncidentResponse> ToActionResult(IncidentResult result)
    {
        return result.Status switch
        {
            IncidentActionStatus.Success => Ok(result.Incident),
            IncidentActionStatus.NotFound => NotFound(new { message = "El incidente no existe." }),
            IncidentActionStatus.EquipmentNotFound => NotFound(new { message = "El equipo no existe." }),
            IncidentActionStatus.EquipmentHasOpenIncident => Conflict(
                new { message = "El equipo ya tiene un incidente abierto (OPEN o IN_PROGRESS)." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}