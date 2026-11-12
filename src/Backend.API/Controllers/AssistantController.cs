using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

// Asistente conversacional para reportar fallas en lenguaje cotidiano (Empleado).
// R5: el asistente solo entiende, pregunta y aconseja. La incidencia la crea el empleado
// cuando confirma (POST /api/incident/PostNewIncident). Las causas probables solo las ve el técnico.
[Authorize(Roles = Roles.AdminOrEmployee)]
[ApiController]
[Route("api/[controller]")]
public class AssistantController : ControllerBase
{
    public const int MaxQuestions = 3;

    private readonly AppDbContext _context;
    private readonly IEquipmentService _equipmentService;
    private readonly IGlossaryService _glossary;
    private readonly IAiClient _aiClient;

    public AssistantController(AppDbContext context, IEquipmentService equipmentService,
        IGlossaryService glossary, IAiClient aiClient)
    {
        _context = context;
        _equipmentService = equipmentService;
        _glossary = glossary;
        _aiClient = aiClient;
    }

    [HttpPost("Triage")]
    public async Task<ActionResult<object>> Triage(TriageChatRequest request, CancellationToken ct)
    {
        if (!await _equipmentService.CanViewAsync(request.EquipmentId, User.Id(), User.Role()))
        {
            return NotFound(new { message = "Solo puedes reportar fallas de los equipos que tienes asignados." });
        }

        if (request.Messages[^1].Role != "empleado")
        {
            return BadRequest(new { message = "El último mensaje debe ser del empleado." });
        }

        var equipment = await _context.Equipments.AsNoTracking()
            .Include(e => e.Components)
            .FirstAsync(e => e.Id == request.EquipmentId, ct);

        // Cada mensaje del asistente en el historial fue una pregunta; el tope lo impone la API.
        var questionsAsked = Math.Min(request.Messages.Count(m => m.Role == "asistente"), MaxQuestions);
        var glossary = await _glossary.GetActiveAsync(ct);

        var aiResponse = await _aiClient.TriageAsync(new AiTriageRequest
        {
            EquipmentContext = new AiEquipmentContext
            {
                Brand = equipment.Brand,
                Model = equipment.Model,
                OsInfo = equipment.OperatingSystem,
                Components = equipment.Components?.Select(c => $"{c.ComponentType} {c.Brand} {c.Model}").ToList() ?? new()
            },
            Messages = request.Messages.Select(m => new AiChatMessage { Role = m.Role, Content = m.Content.Trim() }).ToList(),
            QuestionsAsked = questionsAsked,
            MaxQuestions = MaxQuestions,
            Glossary = glossary.Select(g => new AiGlossaryItem { Expresion = g.Expresion, Significado = g.Significado, Categoria = g.Categoria }).ToList()
        }, ct);

        if (aiResponse is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "El asistente no está disponible en este momento. Puedes reportar la falla escribiéndola directamente." });
        }

        var canAsk = questionsAsked < MaxQuestions && aiResponse.NeedsMoreInfo && !string.IsNullOrWhiteSpace(aiResponse.Question);
        return Ok(new
        {
            understanding = aiResponse.Understanding,
            needsMoreInfo = canAsk,
            question = canAsk ? aiResponse.Question : null,
            questionsRemaining = MaxQuestions - questionsAsked - (canAsk ? 1 : 0),
            safeTips = aiResponse.SafeTips,
            technicalSummary = aiResponse.TechnicalSummary,
            urgency = aiResponse.Urgency,
            requiresTechnician = aiResponse.RequiresTechnician,
            detectedExpressions = aiResponse.DetectedExpressions
        });
    }
}
