using System.Text.Json;
using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class DiagnosisService : IDiagnosisService
{
    private readonly AppDbContext _context;
    private readonly IAiClient _aiClient;
    private readonly ILogger<DiagnosisService> _logger;

    public DiagnosisService(
        AppDbContext context,
        IAiClient aiClient,
        ILogger<DiagnosisService> logger)
    {
        _context = context;
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<DiagnosisResult> DiagnoseAsync(CreateDiagnosisRequest request, Guid currentUserId)
    {
        // 1. Validar equipo
        var equipment = await _context.Equipments
            .AsNoTracking()
            .Include(e => e.Components)
            .FirstOrDefaultAsync(e => e.Id == request.EquipmentId);

        if (equipment is null)
            return new DiagnosisResult(DiagnosisActionStatus.EquipmentNotFound, null);

        // 2. Validar incidencia (si viene)
        if (request.IncidentId is not null)
        {
            var incident = await _context.Incidents
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == request.IncidentId && i.EquipmentId == request.EquipmentId);

            if (incident is null)
                return new DiagnosisResult(DiagnosisActionStatus.IncidentNotFound, null);

            if (incident.Status != IncidentStatus.OPEN)
                return new DiagnosisResult(DiagnosisActionStatus.IncidentNotOpen, null);
        }

        // 3. Cargar biblioteca de errores relevante
        var knowledge = await _context.ErrorKnowledges
            .AsNoTracking()
            .Where(e => e.Active && (e.Symptom.Contains(request.Symptom) || request.Symptom.Contains(e.Symptom)))
            .Take(5)
            .ToListAsync();

        // 4. Construir el request para FastAPI
        var components = equipment.Components?
            .Select(c => $"{c.ComponentType} {c.Brand} {c.Model}")
            .ToList() ?? new List<string>();

        var componentsWithFallback = components.Count > 0
            ? components
            : new List<string> { "dato no disponible" };

        var aiRequest = new AiDiagnosisRequest
        {
            EquipmentId = equipment.Id,
            Symptom = request.Symptom.Trim(),
            EquipmentContext = new AiEquipmentContext
            {
                Brand = string.IsNullOrWhiteSpace(equipment.Brand) ? "dato no disponible" : equipment.Brand,
                Model = string.IsNullOrWhiteSpace(equipment.Model) ? "dato no disponible" : equipment.Model,
                OsInfo = string.IsNullOrWhiteSpace(equipment.OperatingSystem)
                    ? "dato no disponible"
                    : equipment.OperatingSystem,
                Components = componentsWithFallback
            },
            KnowledgeContext = knowledge.Select(k => new AiKnowledgeItem
            {
                Symptom = k.Symptom,
                PossibleCauses = k.PossibleCauses,
                SuggestedSolution = k.SuggestedSolution
            }).ToList()
        };

        // 5. Llamar a la IA
        var aiResponse = await _aiClient.DiagnoseAsync(aiRequest);
        if (aiResponse is null)
            return new DiagnosisResult(DiagnosisActionStatus.AiServiceUnavailable, null);

        if (aiResponse.RankedCauses.Count == 0)
            return new DiagnosisResult(DiagnosisActionStatus.AiResponseInvalid, null);

        // 6. Persistir
        var diagnosis = new AIDiagnosis
        {
            Id = Guid.NewGuid(),
            EquipmentId = equipment.Id,
            IncidentId = request.IncidentId,
            CreatedByUserId = currentUserId,
            Symptom = aiRequest.Symptom,
            RankedCausesJson = JsonSerializer.Serialize(aiResponse.RankedCauses),
            RequiresTechnician = aiResponse.RequiresTechnician,
            Reasoning = aiResponse.Reasoning,
            ModelVersion = "llama3.1:8b",
            Confidence = aiResponse.RankedCauses.Max(c => c.Probability),
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow,

            ContextSnapshotJson = JsonSerializer.Serialize(new
            {
                brand = aiRequest.EquipmentContext.Brand,
                model = aiRequest.EquipmentContext.Model,
                osInfo = aiRequest.EquipmentContext.OsInfo,
                components = aiRequest.EquipmentContext.Components
            })
        };

        _context.AIDiagnoses.Add(diagnosis);
        await _context.SaveChangesAsync();

        // 7. Cargar navegaciones
        await _context.Entry(diagnosis).Reference(d => d.Equipment).LoadAsync();
        await _context.Entry(diagnosis).Reference(d => d.CreatedByUser).LoadAsync();

        _logger.LogInformation(
            "Diagnóstico IA {DiagnosisId} generado para equipo {EquipmentId} ({Confidence:P0})",
            diagnosis.Id, equipment.Id, diagnosis.Confidence);

        return new DiagnosisResult(DiagnosisActionStatus.Success, Map(diagnosis));
    }

    public async Task<DiagnosisDetailsResponse?> GetByIdAsync(Guid id)
    {
        var diagnosis = await _context.AIDiagnoses
            .AsNoTracking()
            .Include(d => d.Equipment)
            .Include(d => d.CreatedByUser)
            .FirstOrDefaultAsync(d => d.Id == id);

        return diagnosis is null ? null : Map(diagnosis);
    }

    public async Task<List<DiagnosisDetailsResponse>> GetByEquipmentAsync(Guid equipmentId)
    {
        var diagnoses = await _context.AIDiagnoses
            .AsNoTracking()
            .Include(d => d.Equipment)
            .Include(d => d.CreatedByUser)
            .Where(d => d.EquipmentId == equipmentId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return diagnoses.Select(Map).ToList();
    }

    public async Task<List<DiagnosisDetailsResponse>> GetRecentAsync(int take = 20)
    {
        var diagnoses = await _context.AIDiagnoses
            .AsNoTracking()
            .Include(d => d.Equipment)
            .Include(d => d.CreatedByUser)
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .ToListAsync();

        return diagnoses.Select(Map).ToList();
    }

    private static DiagnosisDetailsResponse Map(AIDiagnosis d)
    {
        List<DiagnosisRankedCause> causes;
        try
        {
            causes = JsonSerializer.Deserialize<List<DiagnosisRankedCause>>(d.RankedCausesJson) ?? new();
        }
        catch
        {
            causes = new();
        }

        DiagnosisContextSnapshot context = new();
        try
        {
            context = JsonSerializer.Deserialize<DiagnosisContextSnapshot>(d.ContextSnapshotJson) ?? new();
        }
        catch { }

        return new DiagnosisDetailsResponse
        {
            Id = d.Id,
            EquipmentId = d.EquipmentId,
            EquipmentInternalCode = d.Equipment?.InternalCode ?? string.Empty,
            IncidentId = d.IncidentId,
            CreatedByUserId = d.CreatedByUserId,
            CreatedByName = d.CreatedByUser?.FullName ?? string.Empty,
            Symptom = d.Symptom,
            RankedCauses = causes,
            RequiresTechnician = d.RequiresTechnician,
            Reasoning = d.Reasoning,
            ModelVersion = d.ModelVersion,
            Confidence = d.Confidence,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            ContextSnapshot = context
        };
    }
}
