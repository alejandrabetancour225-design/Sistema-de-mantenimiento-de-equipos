using System.ComponentModel.DataAnnotations;
using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

public class MaintenancePlanRequest
{
    [Required]
    public Guid EquipmentId { get; set; }

    public Guid? TechnicianId { get; set; }

    [Required(ErrorMessage = "Describe qué se hace en el mantenimiento.")]
    [StringLength(500, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 104, ErrorMessage = "La periodicidad debe estar entre 1 y 104 semanas.")]
    public int IntervalWeeks { get; set; }

    [Required]
    public DateOnly NextDate { get; set; }

    public bool Active { get; set; } = true;
}

public class RescheduleRequest
{
    [Required]
    public DateOnly NextDate { get; set; }
}

// Cronograma de mantenimiento preventivo (Q11): planes por equipo cada N semanas.
// Consulta: personal interno (Empleado: solo sus equipos). Gestión: Administrador y Técnico.
[Authorize(Roles = Roles.Staff)]
[ApiController]
[Route("api/[controller]")]
public class MaintenancePlansController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEquipmentService _equipment;
    private readonly IMaintenanceService _maintenance;

    public MaintenancePlansController(AppDbContext context, IEquipmentService equipment, IMaintenanceService maintenance)
    {
        _context = context;
        _equipment = equipment;
        _maintenance = maintenance;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetPlans([FromQuery] Guid? equipmentId)
    {
        var visible = (await _equipment.GetAllAsync(User.Id(), User.Role())).Select(e => e.Id).ToHashSet();
        var plans = await _context.MaintenancePlans.AsNoTracking()
            .Include(p => p.Equipment).Include(p => p.Technician)
            .Where(p => equipmentId == null || p.EquipmentId == equipmentId)
            .OrderBy(p => p.NextDate)
            .ToListAsync();
        return Ok(plans.Where(p => visible.Contains(p.EquipmentId)).Select(Map));
    }

    // Posibles responsables de un plan: Técnicos y Administradores activos de la organización.
    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpGet("Technicians")]
    public async Task<ActionResult<object>> Technicians()
        => Ok(await _context.Users.AsNoTracking()
            .Where(u => u.Active && u.Role != null && (u.Role.Name == Roles.Tecnico || u.Role.Name == Roles.Administrador))
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName, Role = u.Role!.Name })
            .ToListAsync());

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost]
    public async Task<ActionResult<object>> Create(MaintenancePlanRequest request)
    {
        var error = await ValidateAsync(request);
        if (error is not null) return error;

        var now = DateTime.UtcNow;
        var plan = new MaintenancePlan
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            TechnicianId = request.TechnicianId,
            Description = request.Description.Trim(),
            IntervalWeeks = request.IntervalWeeks,
            NextDate = request.NextDate,
            Active = request.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.MaintenancePlans.Add(plan);
        await _context.SaveChangesAsync();
        return Ok(new { plan.Id });
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<object>> Update(Guid id, MaintenancePlanRequest request)
    {
        var plan = await _context.MaintenancePlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null) return NotFound(new { message = "El plan no existe." });
        var error = await ValidateAsync(request, plan.NextDate);
        if (error is not null) return error;

        plan.TechnicianId = request.TechnicianId;
        plan.Description = request.Description.Trim();
        plan.IntervalWeeks = request.IntervalWeeks;
        plan.NextDate = request.NextDate;
        plan.Active = request.Active;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { plan.Id });
    }

    // Mover la próxima fecha (arrastrar en el calendario).
    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPatch("{id:guid}/Reschedule")]
    public async Task<ActionResult<object>> Reschedule(Guid id, RescheduleRequest request)
    {
        var plan = await _context.MaintenancePlans.FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null) return NotFound(new { message = "El plan no existe." });
        if (request.NextDate < LocalClock.Today)
        {
            return BadRequest(new { message = "No se puede programar un mantenimiento en una fecha pasada." });
        }

        plan.NextDate = request.NextDate;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { plan.Id, plan.NextDate });
    }

    // Iniciar el mantenimiento programado: abre un preventivo ligado al plan.
    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("{id:guid}/Start")]
    public async Task<ActionResult<MaintenanceResponse>> Start(Guid id)
    {
        var plan = await _context.MaintenancePlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.Active);
        if (plan is null) return NotFound(new { message = "El plan no existe o está inactivo." });

        var result = await _maintenance.CreateAsync(new CreateMaintenanceRequest
        {
            EquipmentId = plan.EquipmentId,
            TechnicianId = plan.TechnicianId ?? User.Id(),
            Type = MaintenanceType.PREVENTIVE,
            ReportedProblem = $"Mantenimiento preventivo programado: {plan.Description}",
            MaintenancePlanId = plan.Id
        });

        return result.Status == MaintenanceActionStatus.Success
            ? Ok(result.Maintenance)
            : BadRequest(new { message = result.Status switch
            {
                MaintenanceActionStatus.EquipmentHasOpenMaintenance => "El equipo ya tiene un mantenimiento abierto.",
                MaintenanceActionStatus.RentedPreventiveNotAllowed => "El contrato de alquiler no permite mantenimiento preventivo interno.",
                MaintenanceActionStatus.EquipmentDecommissioned => "El equipo está dado de baja o devuelto.",
                MaintenanceActionStatus.TechnicianNotFound or MaintenanceActionStatus.TechnicianRoleRequired => "El técnico del plan no está disponible.",
                _ => "No se pudo iniciar el mantenimiento."
            } });
    }

    private async Task<ActionResult?> ValidateAsync(MaintenancePlanRequest request, DateOnly? currentDate = null)
    {
        var equipment = await _context.Equipments.Include(e => e.RentalContract)
            .FirstOrDefaultAsync(e => e.Id == request.EquipmentId);
        if (equipment is null) return NotFound(new { message = "El equipo no existe." });
        if (equipment.Status is EquipmentStatus.DECOMMISSIONED or EquipmentStatus.RETURNED)
        {
            return BadRequest(new { message = "El equipo está dado de baja o devuelto." });
        }

        if (equipment.Ownership == EquipmentOwnership.RENTED && equipment.RentalContract?.AllowsInternalPreventive != true)
        {
            return BadRequest(new { message = "El contrato de alquiler de este equipo no permite mantenimiento preventivo interno." });
        }

        if (request.NextDate < LocalClock.Today && request.NextDate != currentDate)
        {
            return BadRequest(new { message = "La próxima fecha no puede estar en el pasado." });
        }

        if (request.TechnicianId is { } techId && !await _context.Users.AnyAsync(u =>
                u.Id == techId && u.Active && u.Role != null && (u.Role.Name == Roles.Tecnico || u.Role.Name == Roles.Administrador)))
        {
            return BadRequest(new { message = "El responsable debe ser un Técnico o Administrador activo." });
        }

        return null;
    }

    private static object Map(MaintenancePlan p) => new
    {
        p.Id,
        p.EquipmentId,
        EquipmentCode = p.Equipment?.InternalCode,
        EquipmentName = p.Equipment is null ? null : $"{p.Equipment.Type} {p.Equipment.Brand} {p.Equipment.Model}",
        p.TechnicianId,
        TechnicianName = p.Technician?.FullName,
        p.Description,
        p.IntervalWeeks,
        p.NextDate,
        p.Active,
        p.LastCompletedAt,
        Overdue = p.Active && p.NextDate < LocalClock.Today
    };
}
