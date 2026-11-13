using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

// Genera reportes ejecutivos con IA (Hangfire). Corre sin usuario: filtra por la organización
// del reporte de forma explícita.
public class AiReportJob
{
    private readonly AppDbContext _context;
    private readonly HttpClient _http;
    private readonly INotificationService _notifications;
    private readonly ILogger<AiReportJob> _logger;

    public AiReportJob(AppDbContext context, IHttpClientFactory httpFactory, IConfiguration config,
        INotificationService notifications, ILogger<AiReportJob> logger)
    {
        _context = context;
        _http = httpFactory.CreateClient("python-service");
        _http.BaseAddress ??= new Uri(config["Ai:BaseUrl"] ?? "http://localhost:8000");
        _http.Timeout = TimeSpan.FromSeconds(config.GetValue("Ai:ReportTimeoutSeconds", 400));
        _notifications = notifications;
        _logger = logger;
    }

    // Tarea mensual: reporte del mes anterior para cada organización con plan Integraciones.
    public async Task MonthlyAsync()
    {
        var today = LocalClock.Today;
        var from = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        var to = from.AddMonths(1).AddDays(-1);
        var orgs = await _context.Organizations.AsNoTracking()
            .Where(o => o.Active && o.Plan == MembershipPlan.INTEGRACIONES).Select(o => o.Id).ToListAsync();
        foreach (var org in orgs)
        {
            if (await _context.AiReports.IgnoreQueryFilters().AnyAsync(r => r.OrganizationId == org && r.Automatic && r.PeriodFrom == from))
                continue;
            var report = new AiReport
            {
                Id = Guid.NewGuid(), OrganizationId = org, PeriodFrom = from, PeriodTo = to,
                Automatic = true, CreatedAt = DateTime.UtcNow
            };
            _context.AiReports.Add(report);
            await _context.SaveChangesAsync();
            await GenerateAsync(report.Id);
        }
    }

    public async Task GenerateAsync(Guid reportId)
    {
        var report = await _context.AiReports.IgnoreQueryFilters().FirstAsync(r => r.Id == reportId);
        var org = await _context.Organizations.AsNoTracking().FirstAsync(o => o.Id == report.OrganizationId);
        try
        {
            var stats = await BuildStatsAsync(report.OrganizationId, report.PeriodFrom, report.PeriodTo);
            report.StatsJson = stats.ToJsonString();

            // Mercado: precios y problemas comunes de los modelos más usados (fuentes gratuitas, con fecha).
            var queries = stats["topModels"]!.AsArray().Take(3)
                .SelectMany(m => new[] { $"precio {m} Colombia", $"{m} fallas comunes solución" })
                .ToList();
            JsonNode? market = null;
            if (queries.Count > 0)
            {
                var marketResponse = await _http.PostAsJsonAsync("/api/v1/market/search", new { queries, max_results = 3 });
                market = marketResponse.IsSuccessStatusCode ? JsonNode.Parse(await marketResponse.Content.ReadAsStringAsync()) : null;
            }

            report.MarketJson = market?.ToJsonString();
            var response = await _http.PostAsJsonAsync("/api/v1/reports/executive", new
            {
                organization = org.Name,
                period_from = report.PeriodFrom.ToString("yyyy-MM-dd"),
                period_to = report.PeriodTo.ToString("yyyy-MM-dd"),
                stats,
                market = market?["results"] ?? new JsonArray()
            });
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(JsonNode.Parse(body)?["detail"]?.ToString() ?? body);
            }

            report.ContentJson = body;
            report.Pdf = AiReportPdf.Build(org.Name, report, JsonNode.Parse(body)!, stats, market?["available"]?.GetValue<bool>() == true);
            report.Status = "DONE";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo generar el reporte IA {ReportId}", reportId);
            report.Status = "FAILED";
            report.Error = ex.Message.Length > 900 ? ex.Message[..900] : ex.Message;
        }

        report.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Aviso a Gerentes y Administradores (campana + correo).
        var recipients = await _context.Users.AsNoTracking()
            .Where(u => u.OrganizationId == report.OrganizationId && u.Active && u.Role != null
                && (u.Role.Name == Roles.Gerente || u.Role.Name == Roles.Administrador))
            .Select(u => u.Id).ToListAsync();
        if (report.RequestedByUserId is { } requester) recipients.Add(requester);
        await _notifications.NotifyAsync(new NotificationRequest(report.OrganizationId, recipients,
            report.Status == "DONE" ? "Reporte ejecutivo listo" : "No se pudo generar el reporte ejecutivo",
            report.Status == "DONE"
                ? $"El reporte del {report.PeriodFrom:dd/MM/yyyy} al {report.PeriodTo:dd/MM/yyyy} está disponible (también en PDF)."
                : $"Falló la generación del reporte: {report.Error}",
            "/reportes-ia", "reporte", DedupKey: $"aireport:{report.Id}", SendEmail: report.Status == "DONE"));
    }

    // Cifras del periodo. Solo datos agregados (sin datos personales) para el modelo.
    private async Task<JsonObject> BuildStatsAsync(Guid org, DateOnly from, DateOnly to)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var equipment = _context.Equipments.IgnoreQueryFilters().Where(e => e.OrganizationId == org);
        var maintenances = _context.Maintenances.IgnoreQueryFilters()
            .Where(m => m.OrganizationId == org && m.Status == MaintenanceStatus.COMPLETED && m.CompletedAt >= fromUtc && m.CompletedAt <= toUtc);

        var byStatus = await equipment.GroupBy(e => e.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
        var done = await maintenances.Include(m => m.SpareParts).Include(m => m.Equipment).ToListAsync();
        var cost = done.Sum(m => m.LaborCost + m.OtherCosts + (m.SpareParts?.Sum(s => s.Quantity * s.UnitCostAtUse) ?? 0));
        var topFailing = done.Where(m => m.Type == MaintenanceType.CORRECTIVE)
            .GroupBy(m => $"{m.Equipment?.InternalCode} ({m.Equipment?.Brand} {m.Equipment?.Model})")
            .OrderByDescending(g => g.Count()).Take(5).Select(g => $"{g.Key}: {g.Count()} correctivos");
        var topModels = await equipment.Where(e => e.Status != EquipmentStatus.DECOMMISSIONED && e.Status != EquipmentStatus.RETURNED)
            .GroupBy(e => e.Brand + " " + e.Model).OrderByDescending(g => g.Count()).Take(3).Select(g => g.Key).ToListAsync();
        var openIncidents = await _context.Incidents.IgnoreQueryFilters()
            .CountAsync(i => i.OrganizationId == org && (i.Status == IncidentStatus.OPEN || i.Status == IncidentStatus.IN_PROGRESS));
        var today = LocalClock.Today;
        var overduePlans = await _context.MaintenancePlans.IgnoreQueryFilters()
            .CountAsync(p => p.OrganizationId == org && p.Active && p.NextDate < today);
        var nextPlans = await _context.MaintenancePlans.IgnoreQueryFilters()
            .CountAsync(p => p.OrganizationId == org && p.Active && p.NextDate >= today && p.NextDate <= today.AddDays(30));
        var oldEquipment = await equipment.CountAsync(e => e.AcquisitionDate != null && e.AcquisitionDate < today.AddYears(-4)
            && e.Status != EquipmentStatus.DECOMMISSIONED && e.Status != EquipmentStatus.RETURNED);
        var rentals = await _context.RentalContracts.IgnoreQueryFilters().Include(c => c.Rates).Include(c => c.Equipment)
            .Where(c => c.OrganizationId == org && c.Status == RentalContractStatus.ACTIVE).ToListAsync();
        var rentalCost = rentals.Sum(c => (c.Equipment ?? new List<Equipment>()).Where(e => e.Status != EquipmentStatus.RETURNED)
            .Sum(e => c.Rates!.FirstOrDefault(r => string.Equals(r.EquipmentType, e.Type, StringComparison.OrdinalIgnoreCase))?.MonthlyRate ?? 0));

        return new JsonObject
        {
            ["equipmentByStatus"] = new JsonObject(byStatus.Select(s => KeyValuePair.Create(s.Key.ToString(), (JsonNode?)s.Count))),
            ["maintenancesCompleted"] = done.Count,
            ["preventiveCompleted"] = done.Count(m => m.Type == MaintenanceType.PREVENTIVE),
            ["correctiveCompleted"] = done.Count(m => m.Type == MaintenanceType.CORRECTIVE),
            ["maintenanceCostCop"] = cost,
            ["equipmentWithMostFailures"] = new JsonArray(topFailing.Select(t => (JsonNode?)t).ToArray()),
            ["openIncidents"] = openIncidents,
            ["overduePreventivePlans"] = overduePlans,
            ["preventivePlansNext30Days"] = nextPlans,
            ["equipmentOlderThan4Years"] = oldEquipment,
            ["activeRentalContracts"] = rentals.Count,
            ["monthlyRentalCostCop"] = rentalCost,
            ["topModels"] = new JsonArray(topModels.Select(m => (JsonNode?)m).ToArray())
        };
    }
}

public static class AiReportPdf
{
    public static byte[] Build(string organization, AiReport report, JsonNode content, JsonObject stats, bool marketAvailable)
    {
        var document = new Document();
        document.Info.Title = "Reporte ejecutivo";
        var normal = document.Styles["Normal"]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = 10;
        var section = document.AddSection();
        section.PageSetup.TopMargin = Unit.FromCentimeter(2);

        var title = section.AddParagraph($"Reporte ejecutivo – {organization}");
        title.Format.Font.Size = 16;
        title.Format.Font.Bold = true;
        section.AddParagraph($"Periodo: {report.PeriodFrom:dd/MM/yyyy} a {report.PeriodTo:dd/MM/yyyy} · Generado con IA ({content["model"]}) el {DateTime.UtcNow.AddHours(-5):dd/MM/yyyy HH:mm}")
            .Format.Font.Color = Colors.Gray;

        void Heading(string text)
        {
            var p = section.AddParagraph(text);
            p.Format.Font.Bold = true;
            p.Format.Font.Size = 12;
            p.Format.SpaceBefore = Unit.FromPoint(12);
        }

        void Bullets(IEnumerable<string> items)
        {
            foreach (var item in items)
            {
                section.AddParagraph($"• {item}").Format.LeftIndent = Unit.FromCentimeter(0.4);
            }
        }

        Heading("Resumen");
        section.AddParagraph(content["summary"]?.ToString() ?? "");
        Heading("Hallazgos");
        Bullets(content["findings"]?.AsArray().Select(f => f!.ToString()) ?? Array.Empty<string>());
        Heading("Recomendaciones");
        Bullets(content["recommendations"]?.AsArray().Select(r => $"[{r!["priority"]}] {r["text"]}") ?? Array.Empty<string>());
        if (content["alerts"]?.AsArray().Count > 0)
        {
            Heading("Alertas");
            Bullets(content["alerts"]!.AsArray().Select(a => a!.ToString()));
        }

        Heading("Información de mercado");
        var notes = content["market_notes"]?.AsArray() ?? new JsonArray();
        if (notes.Count == 0)
        {
            section.AddParagraph(marketAvailable
                ? "La IA no encontró información de mercado relevante en las fuentes consultadas."
                : "No se pudo consultar información de mercado en este momento (búsqueda web no disponible).");
        }
        else
        {
            Bullets(notes.Select(n => $"{n!["note"]} — Fuente: {n["title"]} ({n["url"]}), consultada {n["retrieved_at"]}"));
        }

        Heading("Cifras del periodo");
        Bullets(stats.Where(kv => kv.Key != "topModels").Select(kv => $"{kv.Key}: {kv.Value?.ToJsonString()}"));
        section.AddParagraph("Este reporte lo redacta una IA a partir de los datos del sistema; las decisiones son de la gerencia.")
            .Format.Font.Color = Colors.Gray;

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream);
        return stream.ToArray();
    }
}
