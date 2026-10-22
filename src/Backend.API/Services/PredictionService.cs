using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class PredictionService : IPredictionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PredictionService> _logger;

    public PredictionService(AppDbContext context, ILogger<PredictionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<FailurePredictionResponse>> GetHighRiskAsync(int take = 10)
    {
        var predictions = await _context.FailurePredictions
            .AsNoTracking()
            .Include(p => p.Equipment)
            .Where(p => p.RiskScore >= 60 && !p.Acknowledged)
            .OrderByDescending(p => p.RiskScore)
            .Take(take)
            .ToListAsync();

        return predictions.Select(Map).ToList();
    }

    public async Task<FailurePredictionResponse?> GetByEquipmentAsync(Guid equipmentId)
    {
        var prediction = await _context.FailurePredictions
            .AsNoTracking()
            .Include(p => p.Equipment)
            .Where(p => p.EquipmentId == equipmentId)
            .OrderByDescending(p => p.PredictedAt)
            .FirstOrDefaultAsync();

        return prediction is null ? null : Map(prediction);
    }

    public async Task<PredictionResult> RefreshForEquipmentAsync(Guid equipmentId)
    {
        var equipment = await _context.Equipments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == equipmentId);

        if (equipment is null)
            return new PredictionResult(PredictionActionStatus.EquipmentNotFound, null);

        var prediction = await CalculateAsync(equipment);
        _context.FailurePredictions.Add(prediction);
        await _context.SaveChangesAsync();

        await _context.Entry(prediction).Reference(p => p.Equipment).LoadAsync();
        return new PredictionResult(PredictionActionStatus.Success, Map(prediction));
    }

    public async Task<int> RefreshAllAsync()
    {
        var equipments = await _context.Equipments
            .AsNoTracking()
            .Where(e => e.Status != EquipmentStatus.DECOMMISSIONED)
            .ToListAsync();

        // Borrar predicciones previas no reconocidas para no acumular
        var oldPreds = await _context.FailurePredictions
            .Where(p => !p.Acknowledged)
            .ToListAsync();
        _context.FailurePredictions.RemoveRange(oldPreds);

        int count = 0;
        foreach (var equipment in equipments)
        {
            var prediction = await CalculateAsync(equipment);
            _context.FailurePredictions.Add(prediction);
            count++;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Refrescadas {Count} predicciones de falla", count);
        return count;
    }

    public async Task<bool> AcknowledgeAsync(Guid predictionId)
    {
        var prediction = await _context.FailurePredictions
            .FirstOrDefaultAsync(p => p.Id == predictionId);

        if (prediction is null) return false;

        prediction.Acknowledged = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<FailurePrediction> CalculateAsync(Equipment equipment)
    {
        var now = DateTime.UtcNow;
        var sixMonthsAgo = now.AddMonths(-6);
        var threeMonthsAgo = now.AddMonths(-3);

        // 1. Correctivos en últimos 6 meses
        var correctiveCount = await _context.Maintenances
            .AsNoTracking()
            .CountAsync(m => m.EquipmentId == equipment.Id
                && m.Type == MaintenanceType.CORRECTIVE
                && m.StartedAt >= sixMonthsAgo);

        // 2. Incidentes en últimos 3 meses
        var incidentCount = await _context.Incidents
            .AsNoTracking()
            .CountAsync(i => i.EquipmentId == equipment.Id
                && i.ReportedAt >= threeMonthsAgo);

        // 3. Edad del equipo (años desde adquisición)
        int ageYears = 0;
        if (equipment.AcquisitionDate.HasValue)
        {
            var today = DateOnly.FromDateTime(now);
            ageYears = today.Year - equipment.AcquisitionDate.Value.Year;
            if (today < equipment.AcquisitionDate.Value.AddYears(ageYears)) ageYears--;
            if (ageYears < 0) ageYears = 0;
        }

        // 4. Días desde último mantenimiento
        var lastMaintenance = await _context.Maintenances
            .AsNoTracking()
            .Where(m => m.EquipmentId == equipment.Id && m.CompletedAt != null)
            .OrderByDescending(m => m.CompletedAt)
            .Select(m => m.CompletedAt)
            .FirstOrDefaultAsync();

        int daysSinceLastMaintenance = lastMaintenance.HasValue
            ? (int)(now - lastMaintenance.Value).TotalDays
            : 999; // Nunca mantenido = riesgo alto

        // 5. Componente con más incidentes
        var riskComponent = incidentCount > 0 ? "General" : "Ninguno";

        // Cálculo del score
        int score = 0;
        score += Math.Min(correctiveCount * 15, 45);      // cap a 45
        score += Math.Min(incidentCount * 10, 30);        // cap a 30
        score += Math.Min(ageYears * 5, 15);              // cap a 15
        if (daysSinceLastMaintenance > 365) score += 20;  // nunca o hace más de 1 año
        score = Math.Min(score, 100);

        // Razón legible
        var reasons = new List<string>();
        if (correctiveCount > 0) reasons.Add($"{correctiveCount} correctivo(s) últimos 6 meses");
        if (incidentCount > 0) reasons.Add($"{incidentCount} incidente(s) últimos 3 meses");
        if (ageYears > 0) reasons.Add($"edad {ageYears} año(s)");
        if (daysSinceLastMaintenance > 365) reasons.Add("sin mantenimiento en más de 1 año");
        if (reasons.Count == 0) reasons.Add("sin historial significativo");

        return new FailurePrediction
        {
            Id = Guid.NewGuid(),
            EquipmentId = equipment.Id,
            RiskScore = score,
            RiskComponent = riskComponent,
            Reason = string.Join(" · ", reasons),
            ModelVersion = "v1",
            Acknowledged = false,
            PredictedAt = now
        };
    }

    private static FailurePredictionResponse Map(FailurePrediction p)
    {
        string level = p.RiskScore >= 61 ? "HIGH" : p.RiskScore >= 31 ? "MEDIUM" : "LOW";

        return new FailurePredictionResponse
        {
            Id = p.Id,
            EquipmentId = p.EquipmentId,
            EquipmentInternalCode = p.Equipment?.InternalCode ?? string.Empty,
            EquipmentBrand = p.Equipment?.Brand ?? string.Empty,
            EquipmentModel = p.Equipment?.Model ?? string.Empty,
            RiskScore = p.RiskScore,
            RiskLevel = level,
            RiskComponent = p.RiskComponent,
            Reason = p.Reason,
            ModelVersion = p.ModelVersion,
            Acknowledged = p.Acknowledged,
            PredictedAt = p.PredictedAt
        };
    }
}
