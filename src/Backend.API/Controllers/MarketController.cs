using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

// Información de mercado de un equipo (Q6, Q29): precio de reemplazo en Colombia y guías de
// mantenimiento/reparación, desde buscadores gratuitos. Siempre con fuente y fecha de consulta.
[Authorize(Roles = Roles.Administrador + "," + Roles.Tecnico + "," + Roles.Gerente)]
[RequiresPlan(MembershipPlan.INTEGRACIONES)]
[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly HttpClient _http;

    public MarketController(AppDbContext context, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _context = context;
        _http = httpFactory.CreateClient("python-service");
        _http.BaseAddress ??= new Uri(config["Ai:BaseUrl"] ?? "http://localhost:8000");
        _http.Timeout = TimeSpan.FromSeconds(90);
    }

    [HttpGet("Equipment/{equipmentId:guid}")]
    public async Task<ActionResult<object>> ForEquipment(Guid equipmentId, CancellationToken ct)
    {
        var equipment = await _context.Equipments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == equipmentId, ct);
        if (equipment is null) return NotFound(new { message = "El equipo no existe." });

        var name = $"{equipment.Brand} {equipment.Model}".Trim();
        var queries = new[]
        {
            $"precio {name} Colombia",
            $"{name} mantenimiento preventivo guía",
            $"{name} fallas comunes cómo reparar"
        };
        try
        {
            var response = await _http.PostAsJsonAsync("/api/v1/market/search", new { queries, max_results = 3 }, ct);
            var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
            return Ok(new { equipment = name, queries, results = body?["results"], retrievedAt = body?["retrieved_at"], available = body?["available"] });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(503, new { message = "La búsqueda de mercado no está disponible en este momento." });
        }
    }
}
