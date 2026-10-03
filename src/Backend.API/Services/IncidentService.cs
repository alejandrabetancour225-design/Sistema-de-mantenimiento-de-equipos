using Backend.API.Data;
using Backend.API.DTOs;
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
        if (await _context.Equipments.AnyAsync(e => e.Id == request.EquipmentId) is false)
        {
            return new IncidentResult(IncidentActionStatus.EquipmentNotFound, null);
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
            ReportedAt = request.ReportedAt ?? DateTime.UtcNow,
            Status = IncidentStatus.OPEN
        };

        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        _context.Entry(incident).Reference(i => i.Equipment).Load();
        _context.Entry(incident).Reference(i => i.Reporter).Load();

        return new IncidentResult(IncidentActionStatus.Success, Map(incident));
    }

    public async Task<IncidentResult> UpdateAsync(Guid id, UpdateIncidentRequest request)
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

        if (request.Description is not null) incident.Description = request.Description.Trim();
        if (request.Status is not null) incident.Status = request.Status.Value;

        await _context.SaveChangesAsync();

        return new IncidentResult(IncidentActionStatus.Success, Map(incident));
    }

    public async Task<IncidentActionStatus> DeleteAsync(Guid id)
    {
        var incident = await _context.Incidents.FirstOrDefaultAsync(i => i.Id == id);
        if (incident is null)
        {
            return IncidentActionStatus.NotFound;
        }

        _context.Incidents.Remove(incident);
        await _context.SaveChangesAsync();

        return IncidentActionStatus.Success;
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