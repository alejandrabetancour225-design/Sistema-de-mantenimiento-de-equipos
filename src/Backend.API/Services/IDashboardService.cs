using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IDashboardService
{
    Task<DashboardEquipmentSummaryResponse> GetEquipmentSummaryAsync();
    Task<List<DashboardOpenIncidentResponse>> GetOpenIncidentsAsync();
    Task<List<DashboardUpcomingMaintenanceResponse>> GetUpcomingMaintenancesAsync(int daysAhead = 30);
}
