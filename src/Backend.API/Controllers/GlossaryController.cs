using System.ComponentModel.DataAnnotations;
using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

public class GlossaryTermRequest
{
    [Required(ErrorMessage = "La expresión es obligatoria.")]
    [StringLength(120, MinimumLength = 2)]
    public string Expression { get; set; } = string.Empty;

    [Required(ErrorMessage = "El significado es obligatorio.")]
    [StringLength(400, MinimumLength = 3)]
    public string Meaning { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Category { get; set; }

    public bool Active { get; set; } = true;
}

// Glosario de modismos que la IA usa para entender descripciones informales.
// Consulta: personal interno. Edición: Técnico y Administrador (aprendizaje continuo, HU-19).
[Authorize(Roles = Roles.Staff)]
[ApiController]
[Route("api/[controller]")]
public class GlossaryController : ControllerBase
{
    private readonly AppDbContext _context;

    public GlossaryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<SlangTerm>>> GetAll()
        => Ok(await _context.SlangTerms.AsNoTracking().OrderBy(t => t.Category).ThenBy(t => t.Expression).ToListAsync());

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost]
    public async Task<ActionResult<SlangTerm>> Create(GlossaryTermRequest request)
    {
        var expression = request.Expression.Trim().ToLowerInvariant();
        if (await ExistsAsync(expression, null))
        {
            return Conflict(new { message = "Esa expresión ya está en el glosario." });
        }

        var now = DateTime.UtcNow;
        var term = new SlangTerm
        {
            Id = Guid.NewGuid(),
            Expression = expression,
            Meaning = request.Meaning.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "general" : request.Category.Trim().ToLowerInvariant(),
            Active = request.Active,
            CreatedByUserId = User.Id(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.SlangTerms.Add(term);
        await _context.SaveChangesAsync();
        return Ok(term);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SlangTerm>> Update(Guid id, GlossaryTermRequest request)
    {
        var term = await _context.SlangTerms.FirstOrDefaultAsync(t => t.Id == id);
        if (term is null)
        {
            return NotFound(new { message = "La expresión no existe." });
        }

        var expression = request.Expression.Trim().ToLowerInvariant();
        if (await ExistsAsync(expression, id))
        {
            return Conflict(new { message = "Esa expresión ya está en el glosario." });
        }

        term.Expression = expression;
        term.Meaning = request.Meaning.Trim();
        term.Category = string.IsNullOrWhiteSpace(request.Category) ? term.Category : request.Category.Trim().ToLowerInvariant();
        term.Active = request.Active;
        term.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(term);
    }

    // Las expresiones base no se borran (se desactivan); las agregadas por el equipo sí.
    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var term = await _context.SlangTerms.FirstOrDefaultAsync(t => t.Id == id);
        if (term is null)
        {
            return NotFound(new { message = "La expresión no existe." });
        }

        if (term.IsBase)
        {
            term.Active = false;
            term.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.SlangTerms.Remove(term);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> ExistsAsync(string expression, Guid? exceptId)
    {
        var normalized = TextNormalizer.Normalize(expression);
        var all = await _context.SlangTerms.AsNoTracking()
            .Where(t => exceptId == null || t.Id != exceptId)
            .Select(t => t.Expression)
            .ToListAsync();
        return all.Any(e => TextNormalizer.Normalize(e) == normalized);
    }
}
