using System.Reflection;
using System.Text.Json;
using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public record GlossaryEntryDto(string Expresion, string Significado, string Categoria);

public interface IGlossaryService
{
    Task<List<GlossaryEntryDto>> GetActiveAsync(CancellationToken ct = default);
    List<GlossaryEntryDto> Match(string text, IEnumerable<GlossaryEntryDto> glossary);
}

public class GlossaryService : IGlossaryService
{
    private readonly AppDbContext _context;

    public GlossaryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<GlossaryEntryDto>> GetActiveAsync(CancellationToken ct = default)
        => await _context.SlangTerms
            .AsNoTracking()
            .Where(t => t.Active)
            .OrderBy(t => t.Expression)
            .Select(t => new GlossaryEntryDto(t.Expression, t.Meaning, t.Category))
            .ToListAsync(ct);

    public List<GlossaryEntryDto> Match(string text, IEnumerable<GlossaryEntryDto> glossary)
    {
        var normalized = TextNormalizer.Normalize(text);
        return glossary
            .Where(g => TextNormalizer.ContainsPhrase(normalized, g.Expresion))
            .OrderByDescending(g => g.Expresion.Length)
            .Take(8)
            .ToList();
    }

    // Carga el glosario base (Data/Seed/glosario_colombia.json, embebido) si la tabla está vacía.
    public static async Task SeedBaseAsync(AppDbContext context, ILogger logger, CancellationToken ct = default)
    {
        if (await context.SlangTerms.AnyAsync(ct))
        {
            return;
        }

        await using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Backend.API.Data.Seed.glosario_colombia.json");
        if (stream is null)
        {
            logger.LogWarning("No se encontró el glosario base embebido.");
            return;
        }

        var entries = await JsonSerializer.DeserializeAsync<List<GlossaryEntryDto>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct) ?? new();
        var now = DateTime.UtcNow;
        context.SlangTerms.AddRange(entries.Select(e => new SlangTerm
        {
            Id = Guid.NewGuid(),
            Expression = e.Expresion.Trim(),
            Meaning = e.Significado.Trim(),
            Category = e.Categoria,
            IsBase = true,
            CreatedAt = now,
            UpdatedAt = now
        }));
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Glosario base de modismos cargado: {Count} expresiones.", entries.Count);
    }
}
