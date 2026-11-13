using System.Security.Claims;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

[Authorize(Roles = Roles.Administrador + "," + Roles.Tecnico + "," + Roles.Empleado)]
[ApiController]
[Route("api/[controller]")]
public class DiagnosisController : ControllerBase
{
    private readonly IDiagnosisService _diagnosisService;
    private readonly IEquipmentService _equipmentService;

    public DiagnosisController(IDiagnosisService diagnosisService, IEquipmentService equipmentService)
    {
        _diagnosisService = diagnosisService;
        _equipmentService = equipmentService;
    }

    [HttpPost("Diagnose")]
    public async Task<ActionResult<DiagnosisDetailsResponse>> Diagnose(CreateDiagnosisRequest request)
    {
        // R2: el Empleado solo consulta a la IA sobre los equipos que tiene asignados.
        if (!await _equipmentService.CanViewAsync(request.EquipmentId, User.Id(), User.Role()))
        {
            return NotFound(new { message = "El equipo no existe." });
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _diagnosisService.DiagnoseAsync(request, userId);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpGet("GetDiagnosisById/{id:guid}")]
    public async Task<ActionResult<DiagnosisDetailsResponse>> GetById(Guid id)
    {
        var diagnosis = await _diagnosisService.GetByIdAsync(id);
        return diagnosis is null ? NotFound() : Ok(diagnosis);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpGet("GetDiagnosesByEquipment/{equipmentId:guid}")]
    public async Task<ActionResult<List<DiagnosisDetailsResponse>>> GetByEquipment(Guid equipmentId)
    {
        return Ok(await _diagnosisService.GetByEquipmentAsync(equipmentId));
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpGet("GetRecentDiagnoses")]
    public async Task<ActionResult<List<DiagnosisDetailsResponse>>> GetRecent([FromQuery] int take = 20)
    {
        return Ok(await _diagnosisService.GetRecentAsync(take));
    }

    private ActionResult<DiagnosisDetailsResponse> ToActionResult(DiagnosisResult result)
    {
        return result.Status switch
        {
            DiagnosisActionStatus.Success => Ok(result.Diagnosis),
            DiagnosisActionStatus.EquipmentNotFound => NotFound(new { message = "El equipo no existe." }),
            DiagnosisActionStatus.IncidentNotFound => NotFound(new { message = "La incidencia no existe o no pertenece a ese equipo." }),
            DiagnosisActionStatus.IncidentNotOpen => BadRequest(new { message = "Solo se puede diagnosticar sobre incidencias OPEN." }),
            DiagnosisActionStatus.AiServiceUnavailable => StatusCode(503, new { message = "El servicio de IA no está disponible." }),
            DiagnosisActionStatus.AiResponseInvalid => StatusCode(503, new { message = "La IA devolvió una respuesta inválida." }),
            _ => StatusCode(500)
        };
    }
}
