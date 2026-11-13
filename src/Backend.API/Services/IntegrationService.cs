using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

// Campos de nuestro modelo de equipo a los que se puede mapear una columna externa.
public static class MappableFields
{
    public static readonly string[] All =
    {
        "internalCode", "serialNumber", "type", "brand", "model", "location",
        "acquisitionDate", "acquisitionPrice", "operatingSystem"
    };
}

public record SyncSummary(int Read, int Created, int Updated, int Skipped, List<string> Errors, bool Truncated, DateTime At);

public interface IIntegrationService
{
    JsonObject BuildSourceSpec(ExternalSource source);
    Task<JsonNode?> CallPythonAsync(string path, JsonObject spec, CancellationToken ct = default);
    Task<SyncSummary> SyncAsync(Guid sourceId, CancellationToken ct = default);
    string EncryptConnection(Guid sourceId, JsonObject connection);
}

// Sincroniza equipos desde una fuente externa (solo lectura sobre la fuente).
// Funciona con o sin usuario (también desde la tarea programada): siempre filtra por la
// organización de la fuente de forma explícita.
public class IntegrationService : IIntegrationService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _http;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<IntegrationService> _logger;

    public IntegrationService(AppDbContext context, HttpClient http, IConfiguration config,
        IEncryptionService encryption, ILogger<IntegrationService> logger)
    {
        _context = context;
        _http = http;
        _http.BaseAddress = new Uri(config["Ai:BaseUrl"] ?? "http://localhost:8000");
        _http.Timeout = TimeSpan.FromSeconds(120);
        _encryption = encryption;
        _logger = logger;
    }

    private static string ConnectionContext(Guid sourceId) => $"external-source:{sourceId}";

    public string EncryptConnection(Guid sourceId, JsonObject connection)
        => _encryption.Encrypt(connection.ToJsonString(), ConnectionContext(sourceId));

    public JsonObject BuildSourceSpec(ExternalSource source)
    {
        var spec = new JsonObject { ["kind"] = source.Kind, ["table"] = source.TableName };
        if (source.Kind == "file")
        {
            spec["file_name"] = source.FileName;
            spec["file_base64"] = source.FileContent is null ? null : Convert.ToBase64String(source.FileContent);
        }
        else if (source.EncryptedConnection is not null)
        {
            spec["connection"] = JsonNode.Parse(_encryption.Decrypt(source.EncryptedConnection, ConnectionContext(source.Id)));
        }

        return spec;
    }

    public async Task<JsonNode?> CallPythonAsync(string path, JsonObject spec, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(path, spec, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = JsonNode.Parse(body)?["detail"]?.ToString() ?? body;
            throw new IntegrationException(detail);
        }

        return JsonNode.Parse(body);
    }

    public async Task<SyncSummary> SyncAsync(Guid sourceId, CancellationToken ct = default)
    {
        var source = await _context.ExternalSources.IgnoreQueryFilters().FirstAsync(s => s.Id == sourceId, ct);
        var org = source.OrganizationId;
        var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(source.MappingJson) ?? new();
        var errors = new List<string>();
        int created = 0, updated = 0, skipped = 0;

        JsonNode? data;
        try
        {
            data = await CallPythonAsync("/api/v1/integrations/extract", BuildSourceSpec(source), ct);
        }
        catch (Exception ex) when (ex is IntegrationException or HttpRequestException or TaskCanceledException)
        {
            var failed = new SyncSummary(0, 0, 0, 0, new List<string> { $"No se pudo leer la fuente: {ex.Message}" }, false, DateTime.UtcNow);
            await SaveSummaryAsync(source, failed, ct);
            return failed;
        }

        var rows = data?["rows"]?.AsArray() ?? new JsonArray();
        var existing = await _context.Equipments.IgnoreQueryFilters()
            .Where(e => e.OrganizationId == org && e.ExternalSourceId == source.Id)
            .ToDictionaryAsync(e => e.ExternalId!, ct);
        var takenCodes = await _context.Equipments.IgnoreQueryFilters()
            .Where(e => e.OrganizationId == org && e.ExternalSourceId != source.Id)
            .Select(e => new { e.InternalCode, e.SerialNumber }).ToListAsync(ct);
        var codes = takenCodes.Select(t => t.InternalCode).ToHashSet();
        var serials = takenCodes.Select(t => t.SerialNumber).ToHashSet();
        var mappedColumns = mapping.Values.Append(source.KeyColumn).ToHashSet();
        var seenKeys = new HashSet<string>();
        var today = LocalClock.Today;
        var now = DateTime.UtcNow;

        foreach (var node in rows)
        {
            if (node is not JsonObject row) continue;
            string? Get(string field) => mapping.TryGetValue(field, out var col) ? Text(row[col]) : null;

            var key = Text(row[source.KeyColumn]);
            if (string.IsNullOrWhiteSpace(key) || !seenKeys.Add(key))
            {
                skipped++;
                if (errors.Count < 30) errors.Add(string.IsNullOrWhiteSpace(key) ? "Fila sin valor en la columna llave." : $"Llave repetida en la fuente: {key}");
                continue;
            }

            var code = Clean(Get("internalCode") ?? key, 50).ToUpperInvariant();
            var serial = Clean(Get("serialNumber") ?? $"EXT-{key}", 100).ToUpperInvariant();
            var acquisition = ParseDate(Get("acquisitionDate"));
            if (acquisition > today)
            {
                if (errors.Count < 30) errors.Add($"{code}: fecha de adquisición futura ({acquisition:yyyy-MM-dd}) ignorada.");
                acquisition = null;
            }

            var attributes = new JsonObject();
            foreach (var (column, value) in row)
            {
                if (!mappedColumns.Contains(column) && value is not null) attributes[column] = value.DeepClone();
            }

            if (!existing.TryGetValue(key, out var equipment))
            {
                if (codes.Contains(code) || serials.Contains(serial))
                {
                    skipped++;
                    if (errors.Count < 30) errors.Add($"{code}: ya existe un equipo propio con ese código o serie; no se importó.");
                    continue;
                }

                equipment = new Equipment
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = org,
                    ExternalSourceId = source.Id,
                    ExternalId = key,
                    Status = EquipmentStatus.AVAILABLE,
                    CreatedAt = now
                };
                _context.Equipments.Add(equipment);
                existing[key] = equipment;
                created++;
            }
            else
            {
                updated++;
            }

            equipment.InternalCode = code;
            equipment.SerialNumber = serial;
            equipment.Type = Clean(Get("type") ?? "Sin tipo", 100);
            equipment.Brand = Clean(Get("brand") ?? "No especificada", 100);
            equipment.Model = Clean(Get("model") ?? "No especificado", 100);
            equipment.Location = Clean(Get("location") ?? "Sin ubicación", 200);
            equipment.OperatingSystem = Get("operatingSystem") is { Length: > 0 } os ? Clean(os, 100) : equipment.OperatingSystem;
            equipment.AcquisitionDate = acquisition ?? equipment.AcquisitionDate;
            equipment.AcquisitionPrice = ParsePrice(Get("acquisitionPrice")) ?? equipment.AcquisitionPrice;
            equipment.ExternalAttributes = attributes.Count > 0 ? attributes.ToJsonString() : null;
            equipment.UpdatedAt = now;
            codes.Add(code);
            serials.Add(serial);
        }

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Sincronización {SourceId} rechazada por la base", source.Id);
            _context.ChangeTracker.Clear();
            source = await _context.ExternalSources.IgnoreQueryFilters().FirstAsync(s => s.Id == sourceId, ct);
            var failed = new SyncSummary(rows.Count, 0, 0, rows.Count,
                new List<string> { "La base rechazó la importación (códigos o series repetidos). Revisa el mapeo." }, false, now);
            await SaveSummaryAsync(source, failed, ct);
            return failed;
        }

        var summary = new SyncSummary(rows.Count, created, updated, skipped, errors,
            data?["truncated"]?.GetValue<bool>() == true, now);
        await SaveSummaryAsync(source, summary, ct);
        return summary;
    }

    private async Task SaveSummaryAsync(ExternalSource source, SyncSummary summary, CancellationToken ct)
    {
        source.LastSyncAt = summary.At;
        source.LastSyncSummary = JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        source.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    private static string? Text(JsonNode? node) => node switch
    {
        null => null,
        JsonValue v when v.TryGetValue<string>(out var s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim(),
        _ => node.ToJsonString().Trim('"')
    };

    private static string Clean(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed.Length > max ? trimmed[..max] : trimmed;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "yyyy/MM/dd" };
        var text = value.Length >= 10 ? value[..10] : value;
        return DateOnly.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

    private static decimal? ParsePrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Replace("$", "").Replace("COP", "").Replace(" ", "");
        // 1.234.567,89 (Colombia) o 1,234,567.89
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d{1,3}(\.\d{3})+(,\d+)?$"))
            text = text.Replace(".", "").Replace(",", ".");
        else
            text = text.Replace(",", "");
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) && price is >= 0 and <= 9_999_999_999m
            ? price
            : null;
    }
}

public class IntegrationException : Exception
{
    public IntegrationException(string message) : base(message) { }
}

// Tarea programada (cada hora): sincroniza las fuentes activas con intervalo vencido.
public class IntegrationSyncJob
{
    private readonly AppDbContext _context;
    private readonly IIntegrationService _integrations;

    public IntegrationSyncJob(AppDbContext context, IIntegrationService integrations)
    {
        _context = context;
        _integrations = integrations;
    }

    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;
        var due = await _context.ExternalSources.AsNoTracking()
            .Where(s => s.Active && s.SyncIntervalHours > 0
                && (s.LastSyncAt == null || s.LastSyncAt <= now.AddHours(-s.SyncIntervalHours)))
            .Select(s => s.Id).ToListAsync();
        foreach (var id in due)
        {
            await _integrations.SyncAsync(id);
        }
    }
}
