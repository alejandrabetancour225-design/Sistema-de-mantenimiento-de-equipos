using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

// HU-15: Reportes gerenciales para Administrador y Cliente (Gerente).
[Authorize(Roles = Roles.Administrador + "," + Roles.Cliente)]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("GetEquipments")]
    public async Task<IActionResult> GetEquipments(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] Guid? assignedTo,
        [FromQuery] string format = "pdf")
    {
        var filter = new ReportFilter
        {
            From = from,
            To = to,
            Type = type,
            Status = status,
            AssignedTo = assignedTo
        };

        var (bytes, contentType, fileName) = await GenerateReport(
            filter, format, "equipos",
            (f, fmt) => _reportService.GenerateEquipmentsReportAsync(f, fmt));

        return File(bytes, contentType, fileName);
    }

    [HttpGet("GetMaintenances")]
    public async Task<IActionResult> GetMaintenances(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] Guid? technicianId,
        [FromQuery] string format = "pdf")
    {
        var filter = new ReportFilter
        {
            From = from,
            To = to,
            Type = type,
            Status = status,
            TechnicianId = technicianId
        };

        var (bytes, contentType, fileName) = await GenerateReport(
            filter, format, "mantenimientos",
            (f, fmt) => _reportService.GenerateMaintenancesReportAsync(f, fmt));

        return File(bytes, contentType, fileName);
    }

    [HttpGet("GetCosts")]
    public async Task<IActionResult> GetCosts(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? equipmentId,
        [FromQuery] string format = "pdf")
    {
        var filter = new ReportFilter
        {
            From = from,
            To = to,
            EquipmentId = equipmentId
        };

        var (bytes, contentType, fileName) = await GenerateReport(
            filter, format, "costos",
            (f, fmt) => _reportService.GenerateCostsReportAsync(f, fmt));

        return File(bytes, contentType, fileName);
    }

    private static async Task<(byte[] Bytes, string ContentType, string FileName)> GenerateReport(
        ReportFilter filter,
        string format,
        string reportName,
        Func<ReportFilter, ReportFormat, Task<byte[]>> generate)
    {
        var fmt = format.Equals("excel", StringComparison.OrdinalIgnoreCase)
            ? ReportFormat.Excel
            : ReportFormat.Pdf;

        var bytes = await generate(filter, fmt);

        var contentType = fmt == ReportFormat.Pdf
            ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        var extension = fmt == ReportFormat.Pdf ? "pdf" : "xlsx";
        var fileName = $"{reportName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{extension}";

        return (bytes, contentType, fileName);
    }
}
