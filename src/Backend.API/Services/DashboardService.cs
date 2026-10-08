using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardEquipmentSummaryResponse> GetEquipmentSummaryAsync()
    {
        var counts = await _context.Equipments
            .AsNoTracking()
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var summary = new DashboardEquipmentSummaryResponse
        {
            Available = counts.FirstOrDefault(c => c.Status == EquipmentStatus.AVAILABLE)?.Count ?? 0,
            InUse = counts.FirstOrDefault(c => c.Status == EquipmentStatus.IN_USE)?.Count ?? 0,
            UnderMaintenance = counts.FirstOrDefault(c => c.Status == EquipmentStatus.UNDER_MAINTENANCE)?.Count ?? 0,
            OutOfService = counts.FirstOrDefault(c => c.Status == EquipmentStatus.OUT_OF_SERVICE)?.Count ?? 0,
            Decommissioned = counts.FirstOrDefault(c => c.Status == EquipmentStatus.DECOMMISSIONED)?.Count ?? 0,
        };
        summary.Total = summary.Available + summary.InUse + summary.UnderMaintenance
            + summary.OutOfService + summary.Decommissioned;

        return summary;
    }

    public async Task<List<DashboardOpenIncidentResponse>> GetOpenIncidentsAsync()
    {
        var now = DateTime.UtcNow;
        var incidents = await _context.Incidents
            .AsNoTracking()
            .Include(i => i.Equipment)
            .Where(i => i.Status == IncidentStatus.OPEN || i.Status == IncidentStatus.IN_PROGRESS)
            .OrderBy(i => i.ReportedAt)
            .ToListAsync();

        return incidents.Select(i => new DashboardOpenIncidentResponse
        {
            Id = i.Id,
            EquipmentInternalCode = i.Equipment?.InternalCode ?? string.Empty,
            Description = i.Description,
            ReportedAt = i.ReportedAt,
            Status = i.Status.ToString(),
            DaysOpen = (int)(now - i.ReportedAt).TotalDays,
            IsUrgent = (now - i.ReportedAt).TotalDays > 3
        }).ToList();
    }

    public async Task<List<DashboardUpcomingMaintenanceResponse>> GetUpcomingMaintenancesAsync(int daysAhead = 30)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limit = today.AddDays(daysAhead);

        var maintenances = await _context.Maintenances
            .AsNoTracking()
            .Include(m => m.Equipment)
            .Include(m => m.Technician)
            .Where(m => m.NextMaintenanceDate != null
                && m.NextMaintenanceDate <= limit
                && m.Status != MaintenanceStatus.CANCELLED)
            .OrderBy(m => m.NextMaintenanceDate)
            .ToListAsync();

        return maintenances.Select(m =>
        {
            var daysUntil = m.NextMaintenanceDate!.Value.DayNumber - today.DayNumber;
            return new DashboardUpcomingMaintenanceResponse
            {
                Id = m.Id,
                EquipmentInternalCode = m.Equipment?.InternalCode ?? string.Empty,
                NextMaintenanceDate = m.NextMaintenanceDate,
                DaysUntil = daysUntil,
                IsOverdue = daysUntil < 0,
                TechnicianName = m.Technician?.FullName ?? string.Empty
            };
        }).ToList();
    }
}
