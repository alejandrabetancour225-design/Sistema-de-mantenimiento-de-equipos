using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

public class SourceConnectionRequest
{
    // file | postgres | mysql | sqlserver | mongodb
    [Required]
    [RegularExpression("^(file|postgres|mysql|sqlserver|mongodb)$", ErrorMessage = "Tipo de fuente no soportado.")]
    public string Kind { get; set; } = "file";

    public string? FileName { get; set; }
    public string? FileBase64 { get; set; }

    [StringLength(130)]
    public string? Table { get; set; }

    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Database { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? SslMode { get; set; }
    // MongoDB: mongodb://usuario:clave@host:27017/base
    public string? Uri { get; set; }
}

public class CreateSourceRequest : SourceConnectionRequest
{
    [Required(ErrorMessage = "Ponle un nombre a la fuente.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    // Campo nuestro -> columna de la fuente (confirmado por el Administrador).
    [Required]
    public Dictionary<string, string> Mapping { get; set; } = new();

    [Required(ErrorMessage = "Elige la columna que identifica cada registro.")]
    public string KeyColumn { get; set; } = string.Empty;

    [Range(0, 720)]
    public int SyncIntervalHours { get; set; }
}

// Bases de datos de terceros (Q3, Q4, Q26, Q27). Solo lectura sobre la fuente: se perfila,
// se confirma el mapeo y se importan/actualizan los equipos. Plan Integraciones, solo Administrador.
[Authorize(Roles = Roles.Administrador)]
[RequiresPlan(MembershipPlan.INTEGRACIONES)]
[ApiController]
[Route("api/[controller]")]
public class IntegrationsController : ControllerBase
{
    private const long MaxFileBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _context;
    private readonly IIntegrationService _integrations;

    public IntegrationsController(AppDbContext context, IIntegrationService integrations)
    {
        _context = context;
        _integrations = integrations;
    }

    // Perfila la fuente y sugiere el mapeo (no guarda nada).
    [HttpPost("Profile")]
    [RequestSizeLimit(8_000_000)]
    public async Task<ActionResult<object>> Profile(SourceConnectionRequest request, CancellationToken ct)
    {
        var (spec, error) = BuildSpec(request);
        if (error is not null) return BadRequest(new { message = error });
        try
        {
            var result = await _integrations.CallPythonAsync("/api/v1/integrations/profile", spec!, ct);
            return Ok(new { fields = MappableFields.All, profile = result });
        }
        catch (IntegrationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new { message = "El servicio de integraciones (python-service) no está disponible." });
        }
    }

    [HttpGet("Sources")]
    public async Task<ActionResult<object>> GetSources()
    {
        var sources = await _context.ExternalSources.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        var counts = await _context.Equipments.Where(e => e.ExternalSourceId != null)
            .GroupBy(e => e.ExternalSourceId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync();
        return Ok(sources.Select(s => new
        {
            s.Id,
            s.Name,
            s.Kind,
            s.TableName,
            s.FileName,
            Mapping = JsonNode.Parse(s.MappingJson),
            s.KeyColumn,
            s.SyncIntervalHours,
            s.LastSyncAt,
            LastSyncSummary = s.LastSyncSummary is null ? null : JsonNode.Parse(s.LastSyncSummary),
            s.Active,
            Equipment = counts.FirstOrDefault(c => c.Id == s.Id)?.Count ?? 0
        }));
    }

    // Guarda la fuente (conexión cifrada) e importa por primera vez.
    [HttpPost("Sources")]
    [RequestSizeLimit(8_000_000)]
    public async Task<ActionResult<object>> CreateSource(CreateSourceRequest request, CancellationToken ct)
    {
        var (spec, error) = BuildSpec(request);
        if (error is not null) return BadRequest(new { message = error });

        var unknown = request.Mapping.Keys.Where(k => !MappableFields.All.Contains(k)).ToList();
        if (unknown.Count > 0) return BadRequest(new { message = $"Campos desconocidos en el mapeo: {string.Join(", ", unknown)}" });
        if (request.Mapping.Values.Distinct().Count() != request.Mapping.Count)
        {
            return BadRequest(new { message = "Una misma columna no puede usarse para dos campos." });
        }

        if (await _context.ExternalSources.AnyAsync(s => s.Name == request.Name.Trim(), ct))
        {
            return Conflict(new { message = "Ya existe una fuente con ese nombre." });
        }

        var now = DateTime.UtcNow;
        var source = new ExternalSource
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Kind = request.Kind,
            TableName = request.Table?.Trim(),
            MappingJson = JsonSerializer.Serialize(request.Mapping.Where(m => !string.IsNullOrWhiteSpace(m.Value))
                .ToDictionary(m => m.Key, m => m.Value)),
            KeyColumn = request.KeyColumn,
            SyncIntervalHours = request.Kind == "file" ? 0 : request.SyncIntervalHours,
            CreatedAt = now,
            UpdatedAt = now
        };
        if (request.Kind == "file")
        {
            source.FileName = request.FileName;
            source.FileContent = Convert.FromBase64String(request.FileBase64!);
        }
        else
        {
            source.EncryptedConnection = _integrations.EncryptConnection(source.Id, (JsonObject)spec!["connection"]!.DeepClone());
        }

        _context.ExternalSources.Add(source);
        await _context.SaveChangesAsync(ct);

        var summary = await _integrations.SyncAsync(source.Id, ct);
        return Ok(new { source.Id, summary });
    }

    [HttpPost("Sources/{id:guid}/Sync")]
    public async Task<ActionResult<object>> Sync(Guid id, CancellationToken ct)
    {
        if (!await _context.ExternalSources.AnyAsync(s => s.Id == id && s.Active, ct))
        {
            return NotFound(new { message = "La fuente no existe o está inactiva." });
        }

        return Ok(await _integrations.SyncAsync(id, ct));
    }

    // Desactiva la fuente: deja de sincronizar; los equipos importados se conservan y vuelven a ser editables.
    [HttpDelete("Sources/{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var source = await _context.ExternalSources.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (source is null) return NotFound(new { message = "La fuente no existe." });
        source.Active = false;
        source.EncryptedConnection = null;
        source.FileContent = null;
        source.UpdatedAt = DateTime.UtcNow;
        await _context.Equipments.Where(e => e.ExternalSourceId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ExternalSourceId, (Guid?)null), ct);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static (JsonObject? Spec, string? Error) BuildSpec(SourceConnectionRequest r)
    {
        var spec = new JsonObject { ["kind"] = r.Kind, ["table"] = r.Table?.Trim() };
        if (r.Kind == "file")
        {
            if (string.IsNullOrWhiteSpace(r.FileBase64) || string.IsNullOrWhiteSpace(r.FileName))
                return (null, "Sube un archivo CSV, Excel o JSON.");
            if (r.FileBase64.Length * 3L / 4 > MaxFileBytes)
                return (null, "El archivo supera los 5 MB.");
            spec["file_name"] = r.FileName;
            spec["file_base64"] = r.FileBase64;
            return (spec, null);
        }

        if (string.IsNullOrWhiteSpace(r.Table)) return (null, "Indica la tabla o colección a leer.");
        if (r.Kind == "mongodb")
        {
            if (string.IsNullOrWhiteSpace(r.Uri)) return (null, "Indica la URI de MongoDB.");
            spec["connection"] = new JsonObject { ["uri"] = r.Uri, ["database"] = r.Database };
            return (spec, null);
        }

        if (string.IsNullOrWhiteSpace(r.Host) || string.IsNullOrWhiteSpace(r.Database) || string.IsNullOrWhiteSpace(r.User))
            return (null, "Completa servidor, base de datos y usuario (de solo lectura).");
        spec["connection"] = new JsonObject
        {
            ["host"] = r.Host.Trim(), ["port"] = r.Port, ["database"] = r.Database.Trim(),
            ["user"] = r.User.Trim(), ["password"] = r.Password, ["sslmode"] = r.SslMode
        };
        return (spec, null);
    }
}
