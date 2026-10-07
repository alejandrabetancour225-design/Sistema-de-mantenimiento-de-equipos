using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly AppDbContext _context;

    public MaintenanceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MaintenanceResponse>> GetAllAsync()
    {
        var maintenances = await QueryWithIncludes()
            .AsNoTracking()
            .OrderByDescending(m => m.StartedAt)
            .ToListAsync();

        return maintenances.Select(Map).ToList();
    }

    public async Task<MaintenanceResponse?> GetByIdAsync(Guid id)
    {
        var maintenance = await QueryWithIncludes()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        return maintenance is null ? null : Map(maintenance);
    }

    public async Task<List<MaintenanceResponse>> GetByEquipmentAsync(Guid equipmentId)
    {
        var maintenances = await QueryWithIncludes()
            .AsNoTracking()
            .Where(m => m.EquipmentId == equipmentId)
            .OrderByDescending(m => m.StartedAt)
            .ToListAsync();

        return maintenances.Select(Map).ToList();
    }

    public async Task<MaintenanceResult> CreateAsync(CreateMaintenanceRequest request)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == request.EquipmentId);
        if (equipment is null)
        {
            return new MaintenanceResult(MaintenanceActionStatus.EquipmentNotFound, null);
        }

        var technicianResult = await ValidateTechnicianAsync(request.TechnicianId);
        if (technicianResult is not null)
        {
            return technicianResult;
        }

        Incident? incident = null;
        if (request.IncidentId is not null)
        {
            incident = await _context.Incidents
                .Include(i => i.Maintenance)
                .FirstOrDefaultAsync(i => i.Id == request.IncidentId);

            if (incident is null)
            {
                return new MaintenanceResult(MaintenanceActionStatus.IncidentNotFound, null);
            }

            // La incidencia debe ser del mismo equipo que el mantenimiento.
            if (incident.EquipmentId != request.EquipmentId)
            {
                return new MaintenanceResult(MaintenanceActionStatus.IncidentEquipmentMismatch, null);
            }
        }

        var startedAt = DateTimeUtc.Normalize(request.StartedAt) ?? DateTime.UtcNow;
        if (!NextDateIsValid(request.NextMaintenanceDate, startedAt))
        {
            return new MaintenanceResult(MaintenanceActionStatus.InvalidDates, null);
        }

        var preconditionError = await ValidateCanOpenMaintenanceAsync(equipment, incident);
        if (preconditionError is not null)
        {
            return new MaintenanceResult(preconditionError.Value, null);
        }

        var now = DateTime.UtcNow;
        var maintenance = new Maintenance
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            IncidentId = request.IncidentId,
            TechnicianId = request.TechnicianId,
            Type = request.Type,
            Status = MaintenanceStatus.OPEN,
            ReportedProblem = request.ReportedProblem.Trim(),
            LaborCost = request.LaborCost,
            OtherCosts = request.OtherCosts,
            NextMaintenanceDate = request.NextMaintenanceDate,
            Observations = request.Observations?.Trim() ?? string.Empty,
            StartedAt = startedAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        return await OpenMaintenanceAsync(maintenance, equipment, incident, now);
    }

    public async Task<MaintenanceResult> CreateFromIncidentAsync(Guid incidentId, Guid currentUserId)
    {
        var incident = await _context.Incidents
            .Include(i => i.Equipment)
            .Include(i => i.Maintenance)
            .FirstOrDefaultAsync(i => i.Id == incidentId);

        if (incident is null)
        {
            return new MaintenanceResult(MaintenanceActionStatus.IncidentNotFound, null);
        }

        if (incident.Equipment is null)
        {
            return new MaintenanceResult(MaintenanceActionStatus.EquipmentNotFound, null);
        }

        var technicianResult = await ValidateTechnicianAsync(currentUserId);
        if (technicianResult is not null)
        {
            return technicianResult;
        }

        var preconditionError = await ValidateCanOpenMaintenanceAsync(incident.Equipment, incident);
        if (preconditionError is not null)
        {
            return new MaintenanceResult(preconditionError.Value, null);
        }

        var now = DateTime.UtcNow;
        var maintenance = new Maintenance
        {
            Id = Guid.NewGuid(),
            EquipmentId = incident.EquipmentId,
            IncidentId = incident.Id,
            TechnicianId = currentUserId,
            Type = MaintenanceType.CORRECTIVE,
            Status = MaintenanceStatus.OPEN,
            ReportedProblem = incident.Description,
            LaborCost = 0m,
            OtherCosts = 0m,
            Observations = string.Empty,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        return await OpenMaintenanceAsync(maintenance, incident.Equipment, incident, now);
    }

    public async Task<MaintenanceResult> UpdateAsync(Guid id, UpdateMaintenanceRequest request)
    {
        var maintenance = await QueryWithIncludes().FirstOrDefaultAsync(m => m.Id == id);
        if (maintenance is null)
        {
            return new MaintenanceResult(MaintenanceActionStatus.NotFound, null);
        }

        if (maintenance.Status is MaintenanceStatus.COMPLETED or MaintenanceStatus.CANCELLED)
        {
            return new MaintenanceResult(MaintenanceActionStatus.InvalidStatusTransition, null);
        }

        // Completar exige el cierre formal (trabajo realizado, estado final del equipo e incidencia).
        if (request.Status == MaintenanceStatus.COMPLETED)
        {
            return new MaintenanceResult(MaintenanceActionStatus.MustUseClose, null);
        }

        var nextDate = request.NextMaintenanceDate ?? maintenance.NextMaintenanceDate;
        if (!NextDateIsValid(nextDate, maintenance.StartedAt))
        {
            return new MaintenanceResult(MaintenanceActionStatus.InvalidDates, null);
        }

        if (request.TechnicianId is not null && request.TechnicianId != maintenance.TechnicianId)
        {
            var technicianResult = await ValidateTechnicianAsync(request.TechnicianId.Value);
            if (technicianResult is not null)
            {
                return technicianResult;
            }

            maintenance.TechnicianId = request.TechnicianId.Value;
        }

        var now = DateTime.UtcNow;
        var observationsHandled = false;

        if (request.Status is not null && request.Status != maintenance.Status)
        {
            maintenance.Status = request.Status.Value;

            // Cancelar libera el equipo y devuelve la incidencia a "abierta" para atenderla de nuevo.
            if (request.Status == MaintenanceStatus.CANCELLED)
            {
                if (maintenance.Equipment is { Status: EquipmentStatus.UNDER_MAINTENANCE })
                {
                    maintenance.Equipment.Status = EquipmentStatus.AVAILABLE;
                    maintenance.Equipment.UpdatedAt = now;
                }

                if (maintenance.Incident is { Status: IncidentStatus.IN_PROGRESS })
                {
                    maintenance.Incident.Status = IncidentStatus.OPEN;
                }

                // Se desvincula la incidencia para poder abrirle un mantenimiento nuevo,
                // dejando constancia en las observaciones.
                if (maintenance.IncidentId is { } unlinkedIncidentId)
                {
                    var note = $"[Cancelado: se desvinculó la incidencia {unlinkedIncidentId}]";
                    var observations = request.Observations?.Trim() ?? maintenance.Observations;
                    maintenance.Observations = string.IsNullOrEmpty(observations) ? note : $"{observations} {note}";
                    maintenance.IncidentId = null;
                    maintenance.Incident = null;
                    observationsHandled = true;
                }
            }
        }

        if (request.ReportedProblem is not null) maintenance.ReportedProblem = request.ReportedProblem.Trim();
        if (request.WorkDone is not null) maintenance.WorkDone = request.WorkDone.Trim();
        if (request.LaborCost is not null) maintenance.LaborCost = request.LaborCost.Value;
        if (request.OtherCosts is not null) maintenance.OtherCosts = request.OtherCosts.Value;
        if (request.NextMaintenanceDate is not null) maintenance.NextMaintenanceDate = request.NextMaintenanceDate;
        if (request.Observations is not null && !observationsHandled)
        {
            maintenance.Observations = request.Observations.Trim();
        }

        maintenance.UpdatedAt = now;
        await _context.SaveChangesAsync();

        await _context.Entry(maintenance).Reference(m => m.Technician).LoadAsync();

        return new MaintenanceResult(MaintenanceActionStatus.Success, Map(maintenance));
    }

    public async Task<MaintenanceResult> CloseAsync(Guid id, CloseMaintenanceRequest request)
    {
        var maintenance = await QueryWithIncludes().FirstOrDefaultAsync(m => m.Id == id);
        if (maintenance is null)
        {
            return new MaintenanceResult(MaintenanceActionStatus.NotFound, null);
        }

        if (maintenance.Status is MaintenanceStatus.COMPLETED or MaintenanceStatus.CANCELLED)
        {
            return new MaintenanceResult(MaintenanceActionStatus.InvalidStatusTransition, null);
        }

        // Al cerrar, el equipo no puede quedar "en mantenimiento" y la incidencia debe quedar resuelta o cerrada.
        if (request.EquipmentFinalStatus == EquipmentStatus.UNDER_MAINTENANCE
            || request.IncidentFinalStatus is IncidentStatus.OPEN or IncidentStatus.IN_PROGRESS)
        {
            return new MaintenanceResult(MaintenanceActionStatus.InvalidFinalStatus, null);
        }

        var nextDate = request.NextMaintenanceDate ?? maintenance.NextMaintenanceDate;
        if (!NextDateIsValid(nextDate, maintenance.StartedAt))
        {
            return new MaintenanceResult(MaintenanceActionStatus.InvalidDates, null);
        }

        var now = DateTime.UtcNow;

        maintenance.WorkDone = request.WorkDone.Trim();
        if (request.LaborCost is not null) maintenance.LaborCost = request.LaborCost.Value;
        if (request.OtherCosts is not null) maintenance.OtherCosts = request.OtherCosts.Value;
        if (request.NextMaintenanceDate is not null) maintenance.NextMaintenanceDate = request.NextMaintenanceDate;
        if (request.Observations is not null) maintenance.Observations = request.Observations.Trim();

        maintenance.Status = MaintenanceStatus.COMPLETED;
        maintenance.CompletedAt = now;
        maintenance.UpdatedAt = now;

        if (maintenance.Incident is not null)
        {
            maintenance.Incident.Status = request.IncidentFinalStatus ?? IncidentStatus.RESOLVED;
        }

        if (maintenance.Equipment is not null)
        {
            maintenance.Equipment.Status = request.EquipmentFinalStatus ?? EquipmentStatus.AVAILABLE;
            maintenance.Equipment.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        await _context.Entry(maintenance).Reference(m => m.Technician).LoadAsync();

        return new MaintenanceResult(MaintenanceActionStatus.Success, Map(maintenance));
    }

    public async Task<MaintenanceResult> AddSparePartAsync(Guid maintenanceId, AddSparePartToMaintenanceRequest request)
    {
        var maintenance = await QueryWithIncludes().FirstOrDefaultAsync(m => m.Id == maintenanceId);
        if (maintenance is null)
        {
            return new MaintenanceResult(MaintenanceActionStatus.NotFound, null);
        }

        if (maintenance.Status is MaintenanceStatus.COMPLETED or MaintenanceStatus.CANCELLED)
        {
            return new MaintenanceResult(MaintenanceActionStatus.InvalidStatusTransition, null);
        }

        var sparePart = await _context.SpareParts.FirstOrDefaultAsync(s => s.Id == request.SparePartId);
        if (sparePart is null)
        {
            return new MaintenanceResult(MaintenanceActionStatus.SparePartNotFound, null);
        }

        if (!sparePart.Active)
        {
            return new MaintenanceResult(MaintenanceActionStatus.SparePartInactive, null);
        }

        if (request.Quantity <= 0)
        {
            return new MaintenanceResult(MaintenanceActionStatus.DuplicateSparePartInMaintenance, null);
        }

        var alreadyAdded = await _context.MaintenanceSpareParts
            .AnyAsync(ms => ms.MaintenanceId == maintenanceId && ms.SparePartId == request.SparePartId);
        if (alreadyAdded)
        {
            return new MaintenanceResult(MaintenanceActionStatus.DuplicateSparePartInMaintenance, null);
        }

        var link = new MaintenanceSparePart
        {
            Id = Guid.NewGuid(),
            MaintenanceId = maintenanceId,
            SparePartId = sparePart.Id,
            Quantity = request.Quantity,
            UnitCostAtUse = sparePart.UnitCost
        };

        _context.MaintenanceSpareParts.Add(link);
        maintenance.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, "IX_MaintenanceSpareParts_MaintenanceId_SparePartId"))
        {
            return new MaintenanceResult(MaintenanceActionStatus.DuplicateSparePartInMaintenance, null);
        }

        // Recargar navegación de repuestos
        await _context.Entry(maintenance).Collection(m => m.SpareParts!).Query()
            .Include(ms => ms.SparePart)
            .LoadAsync();

        return new MaintenanceResult(MaintenanceActionStatus.Success, Map(maintenance));
    }

    public async Task<MaintenanceActionStatus> RemoveSparePartAsync(Guid maintenanceId, Guid maintenanceSparePartId)
    {
        var maintenance = await _context.Maintenances.FirstOrDefaultAsync(m => m.Id == maintenanceId);
        if (maintenance is null)
        {
            return MaintenanceActionStatus.NotFound;
        }

        if (maintenance.Status is MaintenanceStatus.COMPLETED or MaintenanceStatus.CANCELLED)
        {
            return MaintenanceActionStatus.InvalidStatusTransition;
        }

        var link = await _context.MaintenanceSpareParts
            .FirstOrDefaultAsync(ms => ms.Id == maintenanceSparePartId && ms.MaintenanceId == maintenanceId);
        if (link is null)
        {
            return MaintenanceActionStatus.MaintenanceSparePartNotFound;
        }

        _context.MaintenanceSpareParts.Remove(link);
        maintenance.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MaintenanceActionStatus.Success;
    }

    public async Task<MaintenanceActionStatus> DeleteAsync(Guid id)
    {
        var maintenance = await _context.Maintenances
            .Include(m => m.Equipment)
            .Include(m => m.Incident)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (maintenance is null)
        {
            return MaintenanceActionStatus.NotFound;
        }

        // Un mantenimiento completado es historial y costo del equipo: no se borra.
        if (maintenance.Status == MaintenanceStatus.COMPLETED)
        {
            return MaintenanceActionStatus.CannotDeleteCompleted;
        }

        // Si estaba en curso, se deja el equipo y la incidencia como estaban antes de abrirlo.
        if (maintenance.Status is MaintenanceStatus.OPEN or MaintenanceStatus.IN_PROGRESS)
        {
            if (maintenance.Equipment is { Status: EquipmentStatus.UNDER_MAINTENANCE })
            {
                maintenance.Equipment.Status = EquipmentStatus.AVAILABLE;
                maintenance.Equipment.UpdatedAt = DateTime.UtcNow;
            }

            if (maintenance.Incident is { Status: IncidentStatus.IN_PROGRESS })
            {
                maintenance.Incident.Status = IncidentStatus.OPEN;
            }
        }

        _context.Maintenances.Remove(maintenance);
        await _context.SaveChangesAsync();

        return MaintenanceActionStatus.Success;
    }

    public async Task<EquipmentHistoryResponse?> GetHistoryByEquipmentAsync(Guid equipmentId)
    {
        var equipment = await _context.Equipments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == equipmentId);

        if (equipment is null)
        {
            return null;
        }

        var maintenances = await QueryWithIncludes()
            .AsNoTracking()
            .Where(m => m.EquipmentId == equipmentId)
            .OrderByDescending(m => m.StartedAt)
            .ToListAsync();

        var incidents = await _context.Incidents
            .AsNoTracking()
            .Include(i => i.Equipment)
            .Include(i => i.Reporter)
            .Include(i => i.Maintenance)
            .Where(i => i.EquipmentId == equipmentId)
            .OrderByDescending(i => i.ReportedAt)
            .ToListAsync();

        var maintenanceResponses = maintenances.Select(Map).ToList();
        var totalLabor = maintenanceResponses.Sum(m => m.LaborCost);
        var totalSpareParts = maintenanceResponses.Sum(m => m.SparePartsCost);
        var totalOther = maintenanceResponses.Sum(m => m.OtherCosts);

        return new EquipmentHistoryResponse
        {
            EquipmentId = equipment.Id,
            EquipmentInternalCode = equipment.InternalCode,
            TotalLaborCost = totalLabor,
            TotalSparePartsCost = totalSpareParts,
            TotalOtherCosts = totalOther,
            TotalCost = totalLabor + totalSpareParts + totalOther,
            Maintenances = maintenanceResponses,
            Incidents = incidents.Select(MapIncident).ToList()
        };
    }

    // ---------- Helpers ----------

    private IQueryable<Maintenance> QueryWithIncludes()
    {
        return _context.Maintenances
            .Include(m => m.Equipment)
            .Include(m => m.Technician)
            .Include(m => m.Incident)
            .Include(m => m.SpareParts!).ThenInclude(ms => ms.SparePart);
    }

    // Reglas para abrir un mantenimiento: equipo no dado de baja, sin otro mantenimiento abierto,
    // e incidencia (si hay) abierta y sin mantenimiento previo.
    private async Task<MaintenanceActionStatus?> ValidateCanOpenMaintenanceAsync(Equipment equipment, Incident? incident)
    {
        if (equipment.Status == EquipmentStatus.DECOMMISSIONED)
        {
            return MaintenanceActionStatus.EquipmentDecommissioned;
        }

        if (incident is not null)
        {
            if (incident.Maintenance is not null)
            {
                return MaintenanceActionStatus.IncidentAlreadyLinked;
            }

            if (incident.Status is not IncidentStatus.OPEN)
            {
                return MaintenanceActionStatus.IncidentNotOpen;
            }
        }

        var hasOpenMaintenance = await _context.Maintenances.AnyAsync(m =>
            m.EquipmentId == equipment.Id
            && (m.Status == MaintenanceStatus.OPEN || m.Status == MaintenanceStatus.IN_PROGRESS));

        return hasOpenMaintenance ? MaintenanceActionStatus.EquipmentHasOpenMaintenance : null;
    }

    // Guarda el mantenimiento nuevo, pone el equipo en mantenimiento y la incidencia en progreso.
    // Los índices únicos resuelven las peticiones simultáneas que pasaron las validaciones previas.
    private async Task<MaintenanceResult> OpenMaintenanceAsync(
        Maintenance maintenance,
        Equipment equipment,
        Incident? incident,
        DateTime now)
    {
        _context.Maintenances.Add(maintenance);

        // R4: el equipo pasa a En mantenimiento
        equipment.Status = EquipmentStatus.UNDER_MAINTENANCE;
        equipment.UpdatedAt = now;

        // HU-11: la incidencia queda en progreso al enlazarse
        if (incident is not null)
        {
            incident.Status = IncidentStatus.IN_PROGRESS;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, AppDbContext.OpenMaintenancePerEquipmentIndex))
        {
            return new MaintenanceResult(MaintenanceActionStatus.EquipmentHasOpenMaintenance, null);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, "IX_Maintenances_IncidentId"))
        {
            return new MaintenanceResult(MaintenanceActionStatus.IncidentAlreadyLinked, null);
        }

        await _context.Entry(maintenance).Reference(m => m.Equipment).LoadAsync();
        await _context.Entry(maintenance).Reference(m => m.Technician).LoadAsync();

        return new MaintenanceResult(MaintenanceActionStatus.Success, Map(maintenance));
    }

    private static bool NextDateIsValid(DateOnly? nextMaintenanceDate, DateTime startedAt)
    {
        return nextMaintenanceDate is null
            || nextMaintenanceDate.Value >= DateOnly.FromDateTime(startedAt);
    }

    private async Task<MaintenanceResult?> ValidateTechnicianAsync(Guid technicianId)
    {
        var technician = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == technicianId);

        if (technician is null)
        {
            return new MaintenanceResult(MaintenanceActionStatus.TechnicianNotFound, null);
        }

        if (!technician.Active)
        {
            return new MaintenanceResult(MaintenanceActionStatus.TechnicianNotFound, null);
        }

        if (technician.Role?.Name != Roles.Tecnico && technician.Role?.Name != Roles.Administrador)
        {
            return new MaintenanceResult(MaintenanceActionStatus.TechnicianRoleRequired, null);
        }

        return null;
    }

    private static MaintenanceResponse Map(Maintenance maintenance)
    {
        var spareParts = maintenance.SpareParts?
            .Select(ms => new MaintenanceSparePartResponse
            {
                Id = ms.Id,
                SparePartId = ms.SparePartId,
                SparePartName = ms.SparePart?.Name ?? string.Empty,
                Quantity = ms.Quantity,
                UnitCostAtUse = ms.UnitCostAtUse,
                Subtotal = ms.Quantity * ms.UnitCostAtUse
            })
            .ToList() ?? new List<MaintenanceSparePartResponse>();

        var sparePartsCost = spareParts.Sum(s => s.Subtotal);

        return new MaintenanceResponse
        {
            Id = maintenance.Id,
            EquipmentId = maintenance.EquipmentId,
            EquipmentInternalCode = maintenance.Equipment?.InternalCode,
            IncidentId = maintenance.IncidentId,
            TechnicianId = maintenance.TechnicianId,
            TechnicianName = maintenance.Technician?.FullName,
            Type = maintenance.Type,
            Status = maintenance.Status,
            ReportedProblem = maintenance.ReportedProblem,
            WorkDone = maintenance.WorkDone,
            LaborCost = maintenance.LaborCost,
            OtherCosts = maintenance.OtherCosts,
            SparePartsCost = sparePartsCost,
            TotalCost = maintenance.LaborCost + maintenance.OtherCosts + sparePartsCost,
            NextMaintenanceDate = maintenance.NextMaintenanceDate,
            Observations = maintenance.Observations,
            StartedAt = maintenance.StartedAt,
            CompletedAt = maintenance.CompletedAt,
            CreatedAt = maintenance.CreatedAt,
            UpdatedAt = maintenance.UpdatedAt,
            SpareParts = spareParts
        };
    }

    private static IncidentResponse MapIncident(Incident incident)
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
