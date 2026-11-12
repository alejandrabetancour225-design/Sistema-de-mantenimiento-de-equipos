using System.Text.Json.Nodes;
using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

public class GenerateAiReportRequest
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

// Reportes ejecutivos generados por IA (plan Integraciones): automáticos cada mes y bajo demanda.
[Authorize(Roles = Roles.AdminOrManager)]
[RequiresPlan(MembershipPlan.INTEGRACIONES)]
[ApiController]
[Route("api/[controller]")]
public class AiReportsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBackgroundJobClient _jobs;

    public AiReportsController(AppDbContext context, IBackgroundJobClient jobs)
    {
        _context = context;
        _jobs = jobs;
    }

    [HttpGet]
    public async Task<ActionResult<object>> List()
    {
        var reports = await _context.AiReports.AsNoTracking().OrderByDescending(r => r.CreatedAt).Take(50)
            .Select(r => new { r.Id, r.PeriodFrom, r.PeriodTo, r.Status, r.Automatic, r.CreatedAt, r.CompletedAt, r.Error, HasPdf = r.Pdf != null })
            .ToListAsync();
        return Ok(reports);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> Get(Guid id)
    {
        var r = await _context.AiReports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound(new { message = "El reporte no existe." });
        return Ok(new
        {
            r.Id, r.PeriodFrom, r.PeriodTo, r.Status, r.Automatic, r.CreatedAt, r.CompletedAt, r.Error,
            Content = r.ContentJson is null ? null : JsonNode.Parse(r.ContentJson),
            Stats = r.StatsJson is null ? null : JsonNode.Parse(r.StatsJson),
            Market = r.MarketJson is null ? null : JsonNode.Parse(r.MarketJson)
        });
    }

    [HttpGet("{id:guid}/Pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        var r = await _context.AiReports.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Pdf, x.PeriodFrom }).FirstOrDefaultAsync();
        if (r?.Pdf is null) return NotFound(new { message = "El PDF no está disponible." });
        return File(r.Pdf, "application/pdf", $"reporte-ejecutivo-{r.PeriodFrom:yyyy-MM}.pdf");
    }

    // Genera un reporte en segundo plano (en CPU tarda unos minutos). Por defecto, los últimos 30 días.
    [HttpPost("Generate")]
    public async Task<ActionResult<object>> Generate(GenerateAiReportRequest request)
    {
        var to = request.To ?? LocalClock.Today;
        var from = request.From ?? to.AddDays(-30);
        if (from > to || to.DayNumber - from.DayNumber > 366)
        {
            return BadRequest(new { message = "Periodo inválido (máximo un año)." });
        }

        var recent = DateTime.UtcNow.AddMinutes(-20);
        if (await _context.AiReports.AnyAsync(r => r.Status == "PENDING" && r.CreatedAt > recent))
        {
            return Conflict(new { message = "Ya hay un reporte generándose; espera a que termine." });
        }

        var report = new AiReport
        {
            Id = Guid.NewGuid(), PeriodFrom = from, PeriodTo = to, RequestedByUserId = User.Id(), CreatedAt = DateTime.UtcNow
        };
        _context.AiReports.Add(report);
        await _context.SaveChangesAsync();
        _jobs.Enqueue<AiReportJob>(job => job.GenerateAsync(report.Id));
        return Accepted(new { report.Id, report.Status });
    }
}
