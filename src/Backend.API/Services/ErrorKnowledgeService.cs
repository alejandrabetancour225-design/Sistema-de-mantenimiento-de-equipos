using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class ErrorKnowledgeService : IErrorKnowledgeService
{
    private readonly AppDbContext _context;

    public ErrorKnowledgeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ErrorKnowledgeResponse>> GetAllAsync(string? componentType, string? symptom)
    {
        var query = _context.ErrorKnowledges
            .AsNoTracking()
            .Where(e => e.Active);

        if (!string.IsNullOrWhiteSpace(componentType))
            query = query.Where(e => e.ComponentType.Contains(componentType));

        if (!string.IsNullOrWhiteSpace(symptom))
            query = query.Where(e => e.Symptom.Contains(symptom));

        var items = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        return items.Select(Map).ToList();
    }

    public async Task<ErrorKnowledgeResponse?> GetByIdAsync(Guid id)
    {
        var item = await _context.ErrorKnowledges.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return item is null ? null : Map(item);
    }

    public async Task<ErrorKnowledgeResult> CreateAsync(CreateErrorKnowledgeRequest request, Guid currentUserId)
    {
        var symptom = request.Symptom.Trim();

        // Validación simple de duplicados
        if (await _context.ErrorKnowledges.AnyAsync(e => e.Symptom == symptom && e.Active))
            return new ErrorKnowledgeResult(ErrorKnowledgeActionStatus.DuplicateSymptom, null);

        var now = DateTime.UtcNow;
        var entity = new ErrorKnowledge
        {
            Id = Guid.NewGuid(),
            Symptom = symptom,
            ComponentType = request.ComponentType.Trim(),
            PossibleCauses = request.PossibleCauses.Trim(),
            SuggestedSolution = request.SuggestedSolution.Trim(),
            Severity = request.Severity,
            Source = "Manual",
            Confidence = 100,
            Active = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = currentUserId
        };

        _context.ErrorKnowledges.Add(entity);
        await _context.SaveChangesAsync();

        return new ErrorKnowledgeResult(ErrorKnowledgeActionStatus.Success, Map(entity));
    }

    public async Task<ErrorKnowledgeResult> UpdateAsync(Guid id, UpdateErrorKnowledgeRequest request)
    {
        var entity = await _context.ErrorKnowledges.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null) return new ErrorKnowledgeResult(ErrorKnowledgeActionStatus.NotFound, null);

        if (request.Symptom is not null) entity.Symptom = request.Symptom.Trim();
        if (request.ComponentType is not null) entity.ComponentType = request.ComponentType.Trim();
        if (request.PossibleCauses is not null) entity.PossibleCauses = request.PossibleCauses.Trim();
        if (request.SuggestedSolution is not null) entity.SuggestedSolution = request.SuggestedSolution.Trim();
        if (request.Severity is not null) entity.Severity = request.Severity.Value;
        if (request.Active is not null) entity.Active = request.Active.Value;

        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ErrorKnowledgeResult(ErrorKnowledgeActionStatus.Success, Map(entity));
    }

    public async Task<ErrorKnowledgeActionStatus> DeleteAsync(Guid id)
    {
        var entity = await _context.ErrorKnowledges.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null) return ErrorKnowledgeActionStatus.NotFound;

        entity.Active = false; // Soft delete
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ErrorKnowledgeActionStatus.Success;
    }

    private static ErrorKnowledgeResponse Map(ErrorKnowledge e) => new()
    {
        Id = e.Id,
        Symptom = e.Symptom,
        ComponentType = e.ComponentType,
        PossibleCauses = e.PossibleCauses,
        SuggestedSolution = e.SuggestedSolution,
        Severity = e.Severity,
        Source = e.Source,
        Confidence = e.Confidence,
        Active = e.Active,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
