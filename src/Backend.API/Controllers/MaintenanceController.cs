using System.Security.Claims;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

// Consulta del historial: todos los roles. El Empleado solo ve los equipos que tiene asignados (R2).
// Gestión: Administrador y Técnico. Anular un mantenimiento cerrado: solo Administrador.
[Authorize(Roles = Roles.AnyRole)]
[ApiController]
[Route("api/[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly IEquipmentService _equipmentService;

    public MaintenanceController(IMaintenanceService maintenanceService, IEquipmentService equipmentService)
    {
        _maintenanceService = maintenanceService;
        _equipmentService = equipmentService;
    }

    private bool IsEmployee => User.Role() == Roles.Empleado;

    // El Empleado solo puede consultar equipos que tiene asignados; los demás roles, todos.
    private async Task<bool> CanSeeEquipmentAsync(Guid equipmentId)
        => !IsEmployee || await _equipmentService.CanViewAsync(equipmentId, User.Id(), User.Role());

    [HttpGet("GetMaintenanceList")]
    public async Task<ActionResult<List<MaintenanceResponse>>> GetMaintenanceList()
    {
        return Ok(await _maintenanceService.GetAllAsync(IsEmployee ? User.Id() : null));
    }

    [HttpGet("GetMaintenanceById/{id:guid}")]
    public async Task<ActionResult<MaintenanceResponse>> GetMaintenanceById(Guid id)
    {
        var maintenance = await _maintenanceService.GetByIdAsync(id);
        if (maintenance is null || !await CanSeeEquipmentAsync(maintenance.EquipmentId))
        {
            return NotFound();
        }

        return Ok(maintenance);
    }

    [HttpGet("GetMaintenancesByEquipment/{equipmentId:guid}")]
    public async Task<ActionResult<List<MaintenanceResponse>>> GetMaintenancesByEquipment(Guid equipmentId)
    {
        if (!await CanSeeEquipmentAsync(equipmentId))
        {
            return NotFound(new { message = "El equipo no existe." });
        }

        return Ok(await _maintenanceService.GetByEquipmentAsync(equipmentId));
    }

    [HttpGet("GetHistoryByEquipment/{equipmentId:guid}")]
    public async Task<ActionResult<EquipmentHistoryResponse>> GetHistoryByEquipment(Guid equipmentId)
    {
        if (!await CanSeeEquipmentAsync(equipmentId))
        {
            return NotFound(new { message = "El equipo no existe." });
        }

        var history = await _maintenanceService.GetHistoryByEquipmentAsync(equipmentId);
        return history is null ? NotFound(new { message = "El equipo no existe." }) : Ok(history);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("PostNewMaintenance")]
    public async Task<ActionResult<MaintenanceResponse>> PostNewMaintenance(CreateMaintenanceRequest request)
    {
        var result = await _maintenanceService.CreateAsync(request);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("CreateFromIncident/{incidentId:guid}")]
    public async Task<ActionResult<MaintenanceResponse>> CreateFromIncident(Guid incidentId)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _maintenanceService.CreateFromIncidentAsync(incidentId, currentUserId);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPut("PutMaintenance/{id:guid}")]
    public async Task<ActionResult<MaintenanceResponse>> PutMaintenance(Guid id, UpdateMaintenanceRequest request)
    {
        var result = await _maintenanceService.UpdateAsync(id, request);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("CloseMaintenance/{id:guid}")]
    public async Task<ActionResult<MaintenanceResponse>> CloseMaintenance(Guid id, CloseMaintenanceRequest request)
    {
        var result = await _maintenanceService.CloseAsync(id, request);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("AddSparePart/{maintenanceId:guid}")]
    public async Task<ActionResult<MaintenanceResponse>> AddSparePart(Guid maintenanceId, AddSparePartToMaintenanceRequest request)
    {
        var result = await _maintenanceService.AddSparePartAsync(maintenanceId, request);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpDelete("RemoveSparePart/{maintenanceId:guid}/{maintenanceSparePartId:guid}")]
    public async Task<IActionResult> RemoveSparePart(Guid maintenanceId, Guid maintenanceSparePartId)
    {
        var status = await _maintenanceService.RemoveSparePartAsync(maintenanceId, maintenanceSparePartId);
        return status switch
        {
            MaintenanceActionStatus.Success => NoContent(),
            MaintenanceActionStatus.NotFound => NotFound(new { message = "El mantenimiento no existe." }),
            MaintenanceActionStatus.MaintenanceSparePartNotFound => NotFound(new { message = "El repuesto del mantenimiento no existe." }),
            MaintenanceActionStatus.InvalidStatusTransition => BadRequest(new { message = "No se puede modificar un mantenimiento cerrado o cancelado." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [Authorize(Roles = Roles.Administrador)]
    [HttpPost("VoidMaintenance/{id:guid}")]
    public async Task<ActionResult<MaintenanceResponse>> VoidMaintenance(Guid id, VoidMaintenanceRequest request)
    {
        var result = await _maintenanceService.VoidAsync(id, request.Reason, User.Id());
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpDelete("DeleteMaintenance/{id:guid}")]
    public async Task<IActionResult> DeleteMaintenance(Guid id)
    {
        var status = await _maintenanceService.DeleteAsync(id);
        return status switch
        {
            MaintenanceActionStatus.Success => NoContent(),
            MaintenanceActionStatus.NotFound => NotFound(new { message = "El mantenimiento no existe." }),
            MaintenanceActionStatus.CannotDeleteCompleted => Conflict(new { message = "Un mantenimiento completado forma parte del historial y no se puede borrar. Si tiene errores, anúlalo y registra uno nuevo." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private ActionResult<MaintenanceResponse> ToActionResult(MaintenanceResult result)
    {
        return result.Status switch
        {
            MaintenanceActionStatus.Success => Ok(result.Maintenance),
            MaintenanceActionStatus.NotFound => NotFound(new { message = "El mantenimiento no existe." }),
            MaintenanceActionStatus.EquipmentNotFound => NotFound(new { message = "El equipo no existe." }),
            MaintenanceActionStatus.TechnicianNotFound => NotFound(new { message = "El técnico no existe o está inactivo." }),
            MaintenanceActionStatus.TechnicianRoleRequired => BadRequest(new { message = "El usuario asignado debe tener rol Técnico o Administrador." }),
            MaintenanceActionStatus.IncidentNotFound => NotFound(new { message = "La incidencia no existe." }),
            MaintenanceActionStatus.IncidentAlreadyLinked => Conflict(new { message = "La incidencia ya tiene un mantenimiento asociado." }),
            MaintenanceActionStatus.InvalidStatusTransition => BadRequest(new { message = "No se puede modificar un mantenimiento cerrado, cancelado o anulado. Si un mantenimiento cerrado tiene errores, anúlalo y registra uno nuevo." }),
            MaintenanceActionStatus.OnlyCompletedCanBeVoided => BadRequest(new { message = "Solo se puede anular un mantenimiento completado. Uno abierto se cancela." }),
            MaintenanceActionStatus.SparePartNotFound => NotFound(new { message = "El repuesto no existe." }),
            MaintenanceActionStatus.SparePartInactive => BadRequest(new { message = "El repuesto está inactivo." }),
            MaintenanceActionStatus.DuplicateSparePartInMaintenance => Conflict(new { message = "El repuesto ya está asociado a este mantenimiento o la cantidad es inválida." }),
            MaintenanceActionStatus.MaintenanceSparePartNotFound => NotFound(new { message = "El repuesto del mantenimiento no existe." }),
            MaintenanceActionStatus.IncidentEquipmentMismatch => BadRequest(new { message = "La incidencia pertenece a otro equipo." }),
            MaintenanceActionStatus.IncidentNotOpen => BadRequest(new { message = "La incidencia ya está resuelta, cerrada o en atención." }),
            MaintenanceActionStatus.EquipmentDecommissioned => BadRequest(new { message = "El equipo está dado de baja." }),
            MaintenanceActionStatus.EquipmentHasOpenMaintenance => Conflict(new { message = "El equipo ya tiene un mantenimiento abierto o en progreso." }),
            MaintenanceActionStatus.MustUseClose => BadRequest(new { message = "Para completar un mantenimiento usa CloseMaintenance (exige el trabajo realizado)." }),
            MaintenanceActionStatus.InvalidFinalStatus => BadRequest(new { message = "Al cerrar, el equipo vuelve a servicio o queda Fuera de servicio (no puede quedar en mantenimiento ni darse de baja), y la incidencia debe quedar resuelta o cerrada." }),
            MaintenanceActionStatus.InvalidDates => BadRequest(new { message = "La fecha del próximo mantenimiento no puede ser anterior al inicio del mantenimiento." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("CreatePreventiveFromPrediction/{equipmentId:guid}")]
    public async Task<ActionResult<MaintenanceResponse>> CreatePreventiveFromPrediction(
        Guid equipmentId,
        [FromBody] CreatePreventiveFromPredictionRequest request)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _maintenanceService.CreatePreventiveFromPredictionAsync(
            equipmentId, currentUserId, request.ReportedProblem);

        return ToActionResult(result);
    }
}
