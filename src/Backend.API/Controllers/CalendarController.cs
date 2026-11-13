using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

// Eventos del calendario de mantenimiento (Q11, Q38) entre dos fechas:
//  - próximas fechas de cada plan preventivo (y sus repeticiones cada N semanas),
//  - mantenimientos abiertos y completados,
//  - vencimientos de contratos de alquiler (no para el Empleado).
// El Empleado solo ve los eventos de los equipos que tiene asignados.
[Authorize(Roles = Roles.AnyRole)]
[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEquipmentService _equipment;

    public CalendarController(AppDbContext context, IEquipmentService equipment)
    {
        _context = context;
        _equipment = equipment;
    }

    [HttpGet]
    public async Task<ActionResult<object>> Get([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (to < from || to.DayNumber - from.DayNumber > 400)
        {
            return BadRequest(new { message = "Rango de fechas inválido (máximo 400 días)." });
        }

        var role = User.Role();
        HashSet<Guid>? onlyEquipment = role == Roles.Empleado
            ? (await _equipment.GetAllAsync(User.Id(), role)).Select(e => e.Id).ToHashSet()
            : null;
        bool Visible(Guid equipmentId) => onlyEquipment is null || onlyEquipment.Contains(equipmentId);

        var today = LocalClock.Today;
        var events = new List<object>();

        // Planes preventivos y sus repeticiones dentro del rango.
        var plans = await _context.MaintenancePlans.AsNoTracking().Include(p => p.Equipment).Include(p => p.Technician)
            .Where(p => p.Active && p.NextDate <= to).ToListAsync();
        foreach (var plan in plans.Where(p => Visible(p.EquipmentId)))
        {
            var date = plan.NextDate;
            for (var i = 0; i < 60 && date <= to; i++, date = date.AddDays(plan.IntervalWeeks * 7))
            {
                if (date < from) continue;
                var isNext = i == 0;
                events.Add(new
                {
                    id = $"plan:{plan.Id}:{date:yyyy-MM-dd}",
                    kind = "plan",
                    title = $"{plan.Equipment?.InternalCode} · {plan.Description}",
                    date,
                    planId = plan.Id,
                    equipmentId = plan.EquipmentId,
                    technician = plan.Technician?.FullName,
                    // Solo la próxima fecha se puede arrastrar; las siguientes son proyección.
                    editable = isNext,
                    projected = !isNext,
                    overdue = isNext && date < today
                });
            }
        }

        // Mantenimientos abiertos (por fecha de inicio) y completados (por fecha de cierre).
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(-1);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc).AddDays(1);
        var maintenances = await _context.Maintenances.AsNoTracking().Include(m => m.Equipment)
            .Where(m => (m.Status == MaintenanceStatus.OPEN || m.Status == MaintenanceStatus.IN_PROGRESS)
                        || (m.Status == MaintenanceStatus.COMPLETED && m.CompletedAt >= fromUtc && m.CompletedAt <= toUtc))
            .ToListAsync();
        foreach (var m in maintenances.Where(m => Visible(m.EquipmentId)))
        {
            var open = m.Status is MaintenanceStatus.OPEN or MaintenanceStatus.IN_PROGRESS;
            var when = DateOnly.FromDateTime((open ? m.StartedAt : m.CompletedAt!.Value).AddHours(-5));
            if (when < from || when > to) continue;
            events.Add(new
            {
                id = $"mnt:{m.Id}",
                kind = open ? "open" : "done",
                title = $"{m.Equipment?.InternalCode} · {(m.Type == MaintenanceType.PREVENTIVE ? "Preventivo" : "Correctivo")}{(open ? " (en curso)" : "")}",
                date = when,
                maintenanceId = m.Id,
                equipmentId = m.EquipmentId,
                editable = false
            });
        }

        // Vencimiento de contratos de alquiler.
        if (role != Roles.Empleado)
        {
            var contracts = await _context.RentalContracts.AsNoTracking().Include(c => c.Lessor)
                .Where(c => c.Status == RentalContractStatus.ACTIVE && c.EndDate >= from && c.EndDate <= to)
                .ToListAsync();
            events.AddRange(contracts.Select(c => new
            {
                id = $"contract:{c.Id}",
                kind = "contract",
                title = $"Vence contrato {c.ContractNumber} ({c.Lessor?.Name})",
                date = c.EndDate,
                editable = false
            }));
        }

        return Ok(events);
    }
}
