using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

[Authorize(Roles = Roles.Administrador + "," + Roles.Cliente)]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("GetEquipmentSummary")]
    public async Task<ActionResult<DashboardEquipmentSummaryResponse>> GetEquipmentSummary()
    {
        return Ok(await _dashboardService.GetEquipmentSummaryAsync());
    }

    [HttpGet("GetOpenIncidents")]
    public async Task<ActionResult<List<DashboardOpenIncidentResponse>>> GetOpenIncidents()
    {
        return Ok(await _dashboardService.GetOpenIncidentsAsync());
    }

    [HttpGet("GetUpcomingMaintenances")]
    public async Task<ActionResult<List<DashboardUpcomingMaintenanceResponse>>> GetUpcomingMaintenances(
        [FromQuery] int daysAhead = 30)
    {
        return Ok(await _dashboardService.GetUpcomingMaintenancesAsync(daysAhead));
    }
}
