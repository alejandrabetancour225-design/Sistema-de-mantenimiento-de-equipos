using System.Text.Json;
using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class EquipmentComponentService : IEquipmentComponentService
{
    private readonly AppDbContext _context;

    public EquipmentComponentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<EquipmentComponentResponse>> GetAllAsync()
    {
        var components = await _context.EquipmentComponents
            .AsNoTracking()
            .Include(c => c.Equipment)
            .OrderBy(c => c.ComponentType)
            .ThenBy(c => c.Brand)
            .ToListAsync();

        return components.Select(Map).ToList();
    }

    public async Task<EquipmentComponentResponse?> GetByIdAsync(Guid id)
    {
        var component = await _context.EquipmentComponents
            .AsNoTracking()
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.Id == id);

        return component is null ? null : Map(component);
    }

    public async Task<List<EquipmentComponentResponse>> GetByEquipmentAsync(Guid equipmentId)
    {
        var components = await _context.EquipmentComponents
            .AsNoTracking()
            .Include(c => c.Equipment)
            .Where(c => c.EquipmentId == equipmentId)
            .OrderBy(c => c.ComponentType)
            .ThenBy(c => c.Brand)
            .ToListAsync();

        return components.Select(Map).ToList();
    }

    public async Task<EquipmentComponentResult> CreateAsync(CreateEquipmentComponentRequest request)
    {
        if (await _context.Equipments.AnyAsync(e => e.Id == request.EquipmentId) is false)
        {
            return new EquipmentComponentResult(EquipmentComponentActionStatus.EquipmentNotFound, null);
        }

        var component = new EquipmentComponent
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            ComponentType = request.ComponentType.Trim(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            SerialNumber = request.SerialNumber.Trim(),
            Specifications = HasJsonValue(request.Specifications) ? request.Specifications!.Value.GetRawText() : null,
            InstalledAt = request.InstalledAt ?? DateTime.UtcNow
        };

        _context.EquipmentComponents.Add(component);
        await _context.SaveChangesAsync();

        _context.Entry(component).Reference(c => c.Equipment).Load();

        return new EquipmentComponentResult(EquipmentComponentActionStatus.Success, Map(component));
    }

    public async Task<EquipmentComponentResult> UpdateAsync(Guid id, UpdateEquipmentComponentRequest request)
    {
        var component = await _context.EquipmentComponents
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (component is null)
        {
            return new EquipmentComponentResult(EquipmentComponentActionStatus.NotFound, null);
        }

        if (request.ComponentType is not null) component.ComponentType = request.ComponentType.Trim();
        if (request.Brand is not null) component.Brand = request.Brand.Trim();
        if (request.Model is not null) component.Model = request.Model.Trim();
        if (request.SerialNumber is not null) component.SerialNumber = request.SerialNumber.Trim();
        if (HasJsonValue(request.Specifications)) component.Specifications = request.Specifications!.Value.GetRawText();
        if (request.InstalledAt is not null) component.InstalledAt = request.InstalledAt.Value;

        await _context.SaveChangesAsync();

        return new EquipmentComponentResult(EquipmentComponentActionStatus.Success, Map(component));
    }

    public async Task<EquipmentComponentActionStatus> DeleteAsync(Guid id)
    {
        var component = await _context.EquipmentComponents.FirstOrDefaultAsync(c => c.Id == id);
        if (component is null)
        {
            return EquipmentComponentActionStatus.NotFound;
        }

        _context.EquipmentComponents.Remove(component);
        await _context.SaveChangesAsync();

        return EquipmentComponentActionStatus.Success;
    }

    private static bool HasJsonValue(JsonElement? element)
    {
        return element is { } value
            && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);
    }

    private static EquipmentComponentResponse Map(EquipmentComponent component)
    {
        return new EquipmentComponentResponse
        {
            Id = component.Id,
            EquipmentId = component.EquipmentId,
            EquipmentInternalCode = component.Equipment?.InternalCode,
            ComponentType = component.ComponentType,
            Brand = component.Brand,
            Model = component.Model,
            SerialNumber = component.SerialNumber,
            Specifications = ParseJson(component.Specifications),
            InstalledAt = component.InstalledAt
        };
    }

    private static JsonElement? ParseJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        using var document = JsonDocument.Parse(raw);
        return document.RootElement.Clone();
    }
}