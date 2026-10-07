using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class IncidentService : IIncidentService
{
    private readonly AppDbContext _context;

    public IncidentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<IncidentResponse>> GetAllAsync()
    {
        var incidents = await _context.Incidents
            .AsNoTracking()
            .Include(i => i.Equipment)
            .Include(i => i.Reporter)
            .Include(i => i.Maintenance)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();

        return incidents.Select(Map).ToList();
    }

    public async Task<IncidentResponse?> GetByIdAsync(Guid id)
    {
        var incident = await _context.Incidents
            .AsNoTracking()
            .Include(i => i.Equipment)
            .Include(i => i.Reporter)
            .Include(i => i.Maintenance)
            .FirstOrDefaultAsync(i => i.Id == id);

        return incident is null ? null : Map(incident);
    }

    public async Task<List<IncidentResponse>> GetByEquipmentAsync(Guid equipmentId)
    {
        var incidents = await _context.Incidents
            .AsNoTracking()
            .Include(i => i.Equipment)
            .Include(i => i.Reporter)
            .Include(i => i.Maintenance)
            .Where(i => i.EquipmentId == equipmentId)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();

        return incidents.Select(Map).ToList();
    }

    public async Task<IncidentResult> CreateAsync(CreateIncidentRequest request, Guid reportedBy)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == request.EquipmentId);
        if (equipment is null)
        {
            return new IncidentResult(IncidentActionStatus.EquipmentNotFound, null);
        }

        if (equipment.Status == EquipmentStatus.DECOMMISSIONED)
        {
            return new IncidentResult(IncidentActionStatus.EquipmentDecommissioned, null);
        }

        var hasOpenIncident = await _context.Incidents
            .AnyAsync(i => i.EquipmentId == request.EquipmentId
                && (i.Status == IncidentStatus.OPEN || i.Status == IncidentStatus.IN_PROGRESS));
        if (hasOpenIncident)
        {
            return new IncidentResult(IncidentActionStatus.EquipmentHasOpenIncident, null);
        }

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            ReportedBy = reportedBy,
            Description = request.Description.Trim(),
            ReportedAt = DateTimeUtc.Normalize(request.ReportedAt) ?? DateTime.UtcNow,
            Status = IncidentStatus.OPEN
        };

        _context.Incidents.Add(incident);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, AppDbContext.OpenIncidentPerEquipmentIndex))
        {
            // Dos reportes simultáneos sobre el mismo equipo: el índice único dejó pasar solo uno.
            return new IncidentResult(IncidentActionStatus.EquipmentHasOpenIncident, null);
        }

        await _context.Entry(incident).Reference(i => i.Equipment).LoadAsync();
        await _context.Entry(incident).Reference(i => i.Reporter).LoadAsync();

        return new IncidentResult(IncidentActionStatus.Success, Map(incident));
    }

    public async Task<IncidentResult> UpdateAsync(Guid id, UpdateIncidentRequest request, Guid currentUserId, string? currentRole)
    {
        var incident = await _context.Incidents
            .Include(i => i.Equipment)
            .Include(i => i.Reporter)
            .Include(i => i.Maintenance)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident is null)
        {
            return new IncidentResult(IncidentActionStatus.NotFound, null);
        }

        var isEmployee = currentRole == Roles.Empleado;

        // El Empleado solo ve como "existentes" sus propios incidentes al editar.
        if (isEmployee && incident.ReportedBy != currentUserId)
        {
            return new IncidentResult(IncidentActionStatus.NotFound, null);
        }

        // El Empleado solo puede tocar su incidente mientras está abierto y sin mantenimiento.
        if (isEmployee && (incident.Status != IncidentStatus.OPEN || incident.Maintenance is not null))
        {
            return new IncidentResult(IncidentActionStatus.HasMaintenance, null);
        }

        if (request.Status is not null && request.Status.Value != incident.Status)
        {
            var statusError = ValidateStatusChange(incident, request.Status.Value, isEmployee);
            if (statusError is not null)
            {
                return new IncidentResult(statusError.Value, null);
            }

            incident.Status = request.Status.Value;
        }

        if (request.Description is not null) incident.Description = request.Description.Trim();

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, AppDbContext.OpenIncidentPerEquipmentIndex))
        {
            // Reabrir chocaría con otro incidente abierto del mismo equipo.
            return new IncidentResult(IncidentActionStatus.EquipmentHasOpenIncident, null);
        }

        return new IncidentResult(IncidentActionStatus.Success, Map(incident));
    }

    public async Task<IncidentActionStatus> DeleteAsync(Guid id, Guid currentUserId, string? currentRole)
    {
        var incident = await _context.Incidents
            .Include(i => i.Maintenance)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (incident is null)
        {
            return IncidentActionStatus.NotFound;
        }

        var isEmployee = currentRole == Roles.Empleado;
        if (isEmployee && incident.ReportedBy != currentUserId)
        {
            return IncidentActionStatus.NotFound;
        }

        // Un incidente con mantenimiento forma parte del historial del equipo.
        if (incident.Maintenance is not null)
        {
            return IncidentActionStatus.HasMaintenance;
        }

        if (isEmployee && incident.Status != IncidentStatus.OPEN)
        {
            return IncidentActionStatus.InvalidStatusChange;
        }

        _context.Incidents.Remove(incident);
        await _context.SaveChangesAsync();

        return IncidentActionStatus.Success;
    }

    // Reglas de estado:
    //  - IN_PROGRESS lo pone el flujo de mantenimiento, nunca a mano.
    //  - Con un mantenimiento asociado, el estado lo maneja ese mantenimiento.
    //  - El Empleado solo puede retirar su reporte (OPEN -> CLOSED).
    private static IncidentActionStatus? ValidateStatusChange(Incident incident, IncidentStatus newStatus, bool isEmployee)
    {
        if (newStatus == IncidentStatus.IN_PROGRESS)
        {
            return IncidentActionStatus.InvalidStatusChange;
        }

        if (incident.Maintenance is not null)
        {
            return IncidentActionStatus.HasMaintenance;
        }

        if (isEmployee && !(incident.Status == IncidentStatus.OPEN && newStatus == IncidentStatus.CLOSED))
        {
            return IncidentActionStatus.InvalidStatusChange;
        }

        return null;
    }

    private static IncidentResponse Map(Incident incident)
    {
        return new IncidentResponse
        {
            Id = incident.Id,
            EquipmentId = incident.EquipmentId,
            EquipmentInternalCode = incident.Equipment?.InternalCode,
            ReportedBy = incident.ReportedBy,
            ReportedByName = incident.Reporter?.FullName,
            Description = incident.Description,
            ReportedAt = incident.ReportedAt,
            Status = incident.Status,
            MaintenanceId = incident.Maintenance?.Id
        };
    }
}