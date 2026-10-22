using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class FeedbackService : IFeedbackService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(AppDbContext context, ILogger<FeedbackService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FeedbackResult> CreateAsync(CreateFeedbackRequest request, Guid technicianId)
    {
        // 1. Validar diagnóstico
        var diagnosis = await _context.AIDiagnoses
            .FirstOrDefaultAsync(d => d.Id == request.DiagnosisId);

        if (diagnosis is null)
            return new FeedbackResult(FeedbackActionStatus.DiagnosisNotFound, null);

        if (diagnosis.Status != "PENDING")
            return new FeedbackResult(FeedbackActionStatus.DiagnosisNotPending, null);

        // 2. Validar técnico
        var technician = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == technicianId);

        if (technician is null)
            return new FeedbackResult(FeedbackActionStatus.TechnicianNotFound, null);

        // 3. Verificar que no exista feedback previo para ese diagnóstico
        var existing = await _context.AIFeedbacks
            .AnyAsync(f => f.DiagnosisId == request.DiagnosisId);

        if (existing)
            return new FeedbackResult(FeedbackActionStatus.FeedbackAlreadyExists, null);

        // 4. Determinar si la IA acertó (comparación simple, case-insensitive)
        var isCorrect = string.Equals(
            request.PredictedCause.Trim(),
            request.ActualCause.Trim(),
            StringComparison.OrdinalIgnoreCase);

        // 5. Crear el feedback
        var feedback = new AIFeedback
        {
            Id = Guid.NewGuid(),
            DiagnosisId = request.DiagnosisId,
            TechnicianId = technicianId,
            PredictedCause = request.PredictedCause.Trim(),
            ActualCause = request.ActualCause.Trim(),
            IsCorrect = isCorrect,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.AIFeedbacks.Add(feedback);

        // 6. Actualizar el diagnóstico (cerrar el ciclo)
        diagnosis.Status = isCorrect ? "CONFIRMED" : "CORRECTED";

        // 7. Aprendizaje: actualizar la biblioteca de errores
        if (isCorrect)
        {
            // La IA acertó → incrementar confianza de la entrada existente
            var knowledge = await _context.ErrorKnowledges
                .FirstOrDefaultAsync(k => k.Active && k.Symptom == diagnosis.Symptom);

            if (knowledge is not null)
            {
                knowledge.Confidence = Math.Min(100, knowledge.Confidence + 5);
                knowledge.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Biblioteca actualizada: +5 confianza en '{Symptom}' (total {Confidence})",
                    knowledge.Symptom, knowledge.Confidence);
            }
        }
        else
        {
            // La IA falló → crear o actualizar la entrada con la causa real
            var knowledge = await _context.ErrorKnowledges
                .FirstOrDefaultAsync(k => k.Active && k.Symptom == diagnosis.Symptom);

            if (knowledge is null)
            {
                // Crear nueva entrada con la causa confirmada
                var now = DateTime.UtcNow;
                _context.ErrorKnowledges.Add(new ErrorKnowledge
                {
                    Id = Guid.NewGuid(),
                    Symptom = diagnosis.Symptom,
                    ComponentType = "General",
                    PossibleCauses = feedback.ActualCause,
                    SuggestedSolution = "Verificado por técnico en caso real",
                    Severity = ErrorSeverity.MEDIUM,
                    Source = "Auto",
                    Confidence = 80, // Confianza alta porque fue verificado
                    Active = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedByUserId = technicianId
                });

                _logger.LogInformation(
                    "Biblioteca: creada nueva entrada automática para '{Symptom}' con causa '{Cause}'",
                    diagnosis.Symptom, feedback.ActualCause);
            }
            else
            {
                // Ajustar la entrada existente (reducir confianza y añadir la causa real)
                knowledge.Confidence = Math.Max(0, knowledge.Confidence - 10);
                knowledge.PossibleCauses = $"{knowledge.PossibleCauses}, {feedback.ActualCause}";
                knowledge.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation(
                    "Biblioteca: -10 confianza en '{Symptom}' por corrección (total {Confidence})",
                    knowledge.Symptom, knowledge.Confidence);
            }
        }

        await _context.SaveChangesAsync();

        // 8. Cargar navegaciones para la respuesta
        await _context.Entry(feedback).Reference(f => f.Technician).LoadAsync();

        return new FeedbackResult(FeedbackActionStatus.Success, Map(feedback));
    }

    public async Task<List<FeedbackResponse>> GetByDiagnosisAsync(Guid diagnosisId)
    {
        var feedbacks = await _context.AIFeedbacks
            .AsNoTracking()
            .Include(f => f.Technician)
            .Where(f => f.DiagnosisId == diagnosisId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return feedbacks.Select(Map).ToList();
    }

    public async Task<List<FeedbackResponse>> GetRecentAsync(int take = 20)
    {
        var feedbacks = await _context.AIFeedbacks
            .AsNoTracking()
            .Include(f => f.Technician)
            .OrderByDescending(f => f.CreatedAt)
            .Take(take)
            .ToListAsync();

        return feedbacks.Select(Map).ToList();
    }

    public async Task<AIMetricsResponse> GetMetricsAsync()
    {
        var totalDiagnoses = await _context.AIDiagnoses.CountAsync();
        var totalFeedbacks = await _context.AIFeedbacks.CountAsync();
        var correct = await _context.AIFeedbacks.CountAsync(f => f.IsCorrect);
        var incorrect = totalFeedbacks - correct;
        var autoGenerated = await _context.ErrorKnowledges
            .CountAsync(k => k.Source == "Auto");

        return new AIMetricsResponse
        {
            TotalDiagnoses = totalDiagnoses,
            TotalFeedbacks = totalFeedbacks,
            CorrectPredictions = correct,
            IncorrectPredictions = incorrect,
            Accuracy = totalFeedbacks == 0 ? 0 : (double)correct / totalFeedbacks,
            AutoGeneratedKnowledgeEntries = autoGenerated
        };
    }

    private static FeedbackResponse Map(AIFeedback f)
    {
        return new FeedbackResponse
        {
            Id = f.Id,
            DiagnosisId = f.DiagnosisId,
            TechnicianId = f.TechnicianId,
            TechnicianName = f.Technician?.FullName ?? string.Empty,
            PredictedCause = f.PredictedCause,
            ActualCause = f.ActualCause,
            IsCorrect = f.IsCorrect,
            Notes = f.Notes,
            CreatedAt = f.CreatedAt
        };
    }
}
