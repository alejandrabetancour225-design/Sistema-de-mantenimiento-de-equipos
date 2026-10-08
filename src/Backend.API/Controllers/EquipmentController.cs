using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

// Cliente: sin acceso. Lectura: Administrador, Técnico y Empleado. Escritura: Administrador y Técnico.
[Authorize(Roles = Roles.Staff)]
[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _equipmentService;

    public EquipmentController(IEquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    [HttpGet("GetEquipmentList")]
    public async Task<ActionResult<List<EquipmentResponse>>> GetEquipmentList()
    {
        return Ok(await _equipmentService.GetAllAsync());
    }

    [HttpGet("GetEquipmentById/{id:guid}")]
    public async Task<ActionResult<EquipmentResponse>> GetEquipmentById(Guid id)
    {
        var equipment = await _equipmentService.GetByIdAsync(id);
        return equipment is null ? NotFound() : Ok(equipment);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("PostNewEquipment")]
    public async Task<ActionResult<EquipmentResponse>> PostNewEquipment(CreateEquipmentRequest request)
    {
        var result = await _equipmentService.CreateAsync(request);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPut("PutEquipment/{id:guid}")]
    public async Task<ActionResult<EquipmentResponse>> PutEquipment(Guid id, UpdateEquipmentRequest request)
    {
        var result = await _equipmentService.UpdateAsync(id, request);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPatch("PatchEquipmentStatus/{id:guid}")]
    public async Task<ActionResult<EquipmentResponse>> PatchEquipmentStatus(Guid id, ChangeStatusRequest request)
    {
        if (request.Status is null)
        {
            return BadRequest(new { message = $"Estado requerido. Valores válidos: {string.Join(", ", Enum.GetNames<EquipmentStatus>())}." });
        }

        var result = await _equipmentService.ChangeStatusAsync(id, request.Status.Value);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpDelete("DeleteEquipment/{id:guid}")]
    public async Task<IActionResult> DeleteEquipment(Guid id)
    {
        var status = await _equipmentService.DeleteAsync(id);
        return status switch
        {
            EquipmentActionStatus.Success => NoContent(),
            EquipmentActionStatus.NotFound => NotFound(new { message = "El equipo no existe." }),
            EquipmentActionStatus.AlreadyDecommissioned => Conflict(
                new { message = "El equipo ya está dado de baja." }),
            EquipmentActionStatus.HasActiveAssignment => Conflict(
                new { message = "El equipo tiene una asignación activa. Libéralo antes de darlo de baja." }),
            EquipmentActionStatus.HasOpenMaintenance => Conflict(
                new { message = "El equipo tiene un mantenimiento abierto. Ciérralo o cancélalo antes de darlo de baja." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private ActionResult<EquipmentResponse> ToActionResult(EquipmentResult result)
    {
        return result.Status switch
        {
            EquipmentActionStatus.Success => Ok(result.Equipment),
            EquipmentActionStatus.NotFound => NotFound(new { message = "El equipo no existe." }),
            EquipmentActionStatus.DuplicateInternalCode => Conflict(
                new { message = "Ya existe un equipo con ese código interno." }),
            EquipmentActionStatus.DuplicateSerialNumber => Conflict(
                new { message = "Ya existe un equipo con ese número de serie." }),
            EquipmentActionStatus.InvalidDates => BadRequest(
                new { message = "La fecha de fin de garantía no puede ser anterior a la fecha de compra." }),
            EquipmentActionStatus.InvalidStatusChange => BadRequest(
                new { message = "El estado UNDER_MAINTENANCE lo asigna el flujo de mantenimiento, y con un mantenimiento abierto el estado solo cambia al cerrarlo o cancelarlo." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
