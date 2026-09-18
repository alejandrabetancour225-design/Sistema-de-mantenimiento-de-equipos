using System.Text.Json;
using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class EquipmentService : IEquipmentService
{
    private readonly AppDbContext _context;

    public EquipmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<EquipmentResponse>> GetAllAsync()
    {
        var equipments = await _context.Equipments
            .AsNoTracking()
            .OrderBy(e => e.InternalCode)
            .ToListAsync();

        return equipments.Select(Map).ToList();
    }

    public async Task<EquipmentResponse?> GetByIdAsync(Guid id)
    {
        var equipment = await _context.Equipments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        return equipment is null ? null : Map(equipment);
    }

    public async Task<EquipmentResult> CreateAsync(CreateEquipmentRequest request)
    {
        var internalCode = request.InternalCode.Trim();
        var serialNumber = request.SerialNumber.Trim();

        if (await _context.Equipments.AnyAsync(e => e.InternalCode == internalCode))
        {
            return new EquipmentResult(EquipmentActionStatus.DuplicateInternalCode, null);
        }

        if (await _context.Equipments.AnyAsync(e => e.SerialNumber == serialNumber))
        {
            return new EquipmentResult(EquipmentActionStatus.DuplicateSerialNumber, null);
        }

        var now = DateTime.UtcNow;
        var equipment = new Equipment
        {
            Id = Guid.NewGuid(),
            InternalCode = internalCode,
            SerialNumber = serialNumber,
            Type = request.Type.Trim(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Characteristics = HasJsonValue(request.Characteristics) ? request.Characteristics!.Value.GetRawText() : null,
            AcquisitionDate = request.AcquisitionDate,
            AcquisitionPrice = request.AcquisitionPrice,
            WarrantyUntil = request.WarrantyUntil,
            Location = request.Location.Trim(),
            Status = request.Status ?? EquipmentStatus.AVAILABLE,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        return new EquipmentResult(EquipmentActionStatus.Success, Map(equipment));
    }

    public async Task<EquipmentResult> UpdateAsync(Guid id, UpdateEquipmentRequest request)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return new EquipmentResult(EquipmentActionStatus.NotFound, null);
        }

        if (request.InternalCode is not null)
        {
            var internalCode = request.InternalCode.Trim();
            if (await _context.Equipments.AnyAsync(e => e.InternalCode == internalCode && e.Id != id))
            {
                return new EquipmentResult(EquipmentActionStatus.DuplicateInternalCode, null);
            }

            equipment.InternalCode = internalCode;
        }

        if (request.SerialNumber is not null)
        {
            var serialNumber = request.SerialNumber.Trim();
            if (await _context.Equipments.AnyAsync(e => e.SerialNumber == serialNumber && e.Id != id))
            {
                return new EquipmentResult(EquipmentActionStatus.DuplicateSerialNumber, null);
            }

            equipment.SerialNumber = serialNumber;
        }

        if (request.Type is not null) equipment.Type = request.Type.Trim();
        if (request.Brand is not null) equipment.Brand = request.Brand.Trim();
        if (request.Model is not null) equipment.Model = request.Model.Trim();
        if (HasJsonValue(request.Characteristics)) equipment.Characteristics = request.Characteristics!.Value.GetRawText();
        if (request.AcquisitionDate is not null) equipment.AcquisitionDate = request.AcquisitionDate;
        if (request.AcquisitionPrice is not null) equipment.AcquisitionPrice = request.AcquisitionPrice;
        if (request.WarrantyUntil is not null) equipment.WarrantyUntil = request.WarrantyUntil;
        if (request.Location is not null) equipment.Location = request.Location.Trim();
        if (request.Status is not null) equipment.Status = request.Status.Value;

        equipment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new EquipmentResult(EquipmentActionStatus.Success, Map(equipment));
    }

    public async Task<EquipmentResult> ChangeStatusAsync(Guid id, EquipmentStatus status)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return new EquipmentResult(EquipmentActionStatus.NotFound, null);
        }

        equipment.Status = status;
        equipment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new EquipmentResult(EquipmentActionStatus.Success, Map(equipment));
    }

    public async Task<EquipmentActionStatus> DeleteAsync(Guid id)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return EquipmentActionStatus.NotFound;
        }

        _context.Equipments.Remove(equipment);
        await _context.SaveChangesAsync();

        return EquipmentActionStatus.Success;
    }

    private static bool HasJsonValue(JsonElement? element)
    {
        return element is { } value
            && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);
    }

    private static EquipmentResponse Map(Equipment equipment)
    {
        return new EquipmentResponse
        {
            Id = equipment.Id,
            InternalCode = equipment.InternalCode,
            SerialNumber = equipment.SerialNumber,
            Type = equipment.Type,
            Brand = equipment.Brand,
            Model = equipment.Model,
            Characteristics = ParseCharacteristics(equipment.Characteristics),
            AcquisitionDate = equipment.AcquisitionDate,
            AcquisitionPrice = equipment.AcquisitionPrice,
            WarrantyUntil = equipment.WarrantyUntil,
            Location = equipment.Location,
            Status = equipment.Status,
            CreatedAt = equipment.CreatedAt,
            UpdatedAt = equipment.UpdatedAt
        };
    }

    private static JsonElement? ParseCharacteristics(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        using var document = JsonDocument.Parse(raw);
        return document.RootElement.Clone();
    }
}