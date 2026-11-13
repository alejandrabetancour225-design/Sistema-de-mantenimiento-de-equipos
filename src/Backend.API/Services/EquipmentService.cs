using System.Text.Json;
using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
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

    public async Task<List<EquipmentResponse>> GetAllAsync(Guid currentUserId, string? role)
    {
        var query = _context.Equipments.AsNoTracking();

        // R2: el Empleado solo ve los equipos que tiene asignados.
        if (role == Roles.Empleado)
        {
            query = query.Where(e => e.Assignments!.Any(a =>
                a.UserId == currentUserId && a.Status == AssignmentStatus.ACTIVE));
        }

        var equipments = await query.Include(e => e.RentalContract).ThenInclude(c => c!.Lessor)
            .OrderBy(e => e.InternalCode).ToListAsync();
        return equipments.Select(Map).ToList();
    }

    public async Task<EquipmentResponse?> GetByIdAsync(Guid id, Guid currentUserId, string? role)
    {
        if (!await CanViewAsync(id, currentUserId, role))
        {
            return null;
        }

        var equipment = await _context.Equipments
            .AsNoTracking()
            .Include(e => e.RentalContract).ThenInclude(c => c!.Lessor)
            .FirstOrDefaultAsync(e => e.Id == id);

        return equipment is null ? null : Map(equipment);
    }

    public async Task<bool> CanViewAsync(Guid equipmentId, Guid currentUserId, string? role)
    {
        if (role is Roles.Administrador or Roles.Tecnico)
        {
            return true;
        }

        if (role == Roles.Empleado)
        {
            return await _context.Assignments.AnyAsync(a =>
                a.EquipmentId == equipmentId
                && a.UserId == currentUserId
                && a.Status == AssignmentStatus.ACTIVE);
        }

        return false;
    }

    public async Task<EquipmentResult> CreateAsync(CreateEquipmentRequest request)
    {
        var internalCode = NormalizeIdentifier(request.InternalCode);
        var serialNumber = NormalizeIdentifier(request.SerialNumber);

        var rental = await ResolveRentalAsync(request.Ownership, request.RentalContractId, request.Type);
        if (rental.Error is not null)
        {
            return new EquipmentResult(rental.Error.Value, null);
        }

        // Alquilado: la "compra" es el inicio del contrato; no hay precio ni garantía propios.
        var acquisitionDate = rental.Contract?.StartDate ?? request.AcquisitionDate;
        var acquisitionPrice = rental.Contract is null ? request.AcquisitionPrice : null;
        var warrantyUntil = rental.Contract is null ? request.WarrantyUntil : null;

        var dateError = rental.Contract is null ? ValidateDates(acquisitionDate, warrantyUntil) : null;
        if (dateError is not null)
        {
            return new EquipmentResult(dateError.Value, null);
        }

        // Un equipo nuevo solo nace Disponible o Fuera de servicio (R4): no tiene asignación ni mantenimiento.
        var initialStatus = request.Status ?? EquipmentStatus.AVAILABLE;
        if (!EquipmentStateMachine.IsValidInitialStatus(initialStatus))
        {
            return new EquipmentResult(EquipmentActionStatus.InvalidStatusChange, null);
        }

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
            OperatingSystem = EmptyToNull(request.OperatingSystem),
            AcquisitionDate = acquisitionDate,
            AcquisitionPrice = acquisitionPrice,
            WarrantyUntil = warrantyUntil,
            Location = request.Location.Trim(),
            Status = initialStatus,
            Ownership = request.Ownership,
            RentalContractId = rental.Contract?.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Equipments.Add(equipment);
        var duplicate = await SaveDetectingDuplicatesAsync();
        if (duplicate is not null)
        {
            return new EquipmentResult(duplicate.Value, null);
        }

        return new EquipmentResult(EquipmentActionStatus.Success, Map(equipment));
    }

    // Corrección de datos (reemplazo completo). Cada cambio queda auditado con valor anterior y nuevo (R1).
    public async Task<EquipmentResult> UpdateAsync(Guid id, UpdateEquipmentRequest request, bool isAdmin)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return new EquipmentResult(EquipmentActionStatus.NotFound, null);
        }

        var internalCode = NormalizeIdentifier(request.InternalCode);
        var serialNumber = NormalizeIdentifier(request.SerialNumber);

        // Q27: los campos mapeados desde una base de terceros se actualizan desde la fuente, no aquí.
        var locked = await GetLockedFieldsAsync(equipment);
        bool Changed(string field) => field switch
        {
            "internalCode" => internalCode != equipment.InternalCode,
            "serialNumber" => serialNumber != equipment.SerialNumber,
            "type" => request.Type.Trim() != equipment.Type,
            "brand" => request.Brand.Trim() != equipment.Brand,
            "model" => request.Model.Trim() != equipment.Model,
            "location" => request.Location.Trim() != equipment.Location,
            "acquisitionDate" => request.AcquisitionDate != equipment.AcquisitionDate,
            "acquisitionPrice" => request.AcquisitionPrice != equipment.AcquisitionPrice,
            "operatingSystem" => EmptyToNull(request.OperatingSystem) != equipment.OperatingSystem,
            _ => false
        };
        if (locked.Any(Changed))
        {
            return new EquipmentResult(EquipmentActionStatus.ExternalFieldLocked, null);
        }

        // Los campos de identidad solo los corrige un Administrador.
        var identityChanged = internalCode != equipment.InternalCode || serialNumber != equipment.SerialNumber;
        if (identityChanged && !isAdmin)
        {
            return new EquipmentResult(EquipmentActionStatus.IdentityChangeForbidden, null);
        }

        var rental = await ResolveRentalAsync(request.Ownership, request.RentalContractId, request.Type,
            keepContractId: equipment.RentalContractId);
        if (rental.Error is not null)
        {
            return new EquipmentResult(rental.Error.Value, null);
        }

        var acquisitionDate = rental.Contract?.StartDate ?? request.AcquisitionDate;
        var dateError = rental.Contract is null ? ValidateDates(acquisitionDate, request.WarrantyUntil) : null;
        if (dateError is not null)
        {
            return new EquipmentResult(dateError.Value, null);
        }

        if (internalCode != equipment.InternalCode
            && await _context.Equipments.AnyAsync(e => e.InternalCode == internalCode && e.Id != id))
        {
            return new EquipmentResult(EquipmentActionStatus.DuplicateInternalCode, null);
        }

        if (serialNumber != equipment.SerialNumber
            && await _context.Equipments.AnyAsync(e => e.SerialNumber == serialNumber && e.Id != id))
        {
            return new EquipmentResult(EquipmentActionStatus.DuplicateSerialNumber, null);
        }

        equipment.InternalCode = internalCode;
        equipment.SerialNumber = serialNumber;
        equipment.Type = request.Type.Trim();
        equipment.Brand = request.Brand.Trim();
        equipment.Model = request.Model.Trim();
        equipment.Characteristics = HasJsonValue(request.Characteristics) ? request.Characteristics!.Value.GetRawText() : null;
        equipment.OperatingSystem = EmptyToNull(request.OperatingSystem);
        equipment.AcquisitionDate = acquisitionDate;
        equipment.AcquisitionPrice = rental.Contract is null ? request.AcquisitionPrice : null;
        equipment.WarrantyUntil = rental.Contract is null ? request.WarrantyUntil : null;
        equipment.Ownership = request.Ownership;
        equipment.RentalContractId = rental.Contract?.Id;
        equipment.Location = request.Location.Trim();
        equipment.UpdatedAt = DateTime.UtcNow;

        var duplicate = await SaveDetectingDuplicatesAsync();
        if (duplicate is not null)
        {
            return new EquipmentResult(duplicate.Value, null);
        }

        return new EquipmentResult(EquipmentActionStatus.Success, Map(equipment));
    }

    // Cambio manual de estado: solo Fuera de servicio <-> en servicio (R4).
    // Para volver a servicio basta pedir AVAILABLE o IN_USE: el estado real lo decide la asignación activa.
    public async Task<EquipmentResult> ChangeStatusAsync(Guid id, EquipmentStatus status)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == id);
        if (equipment is null)
        {
            return new EquipmentResult(EquipmentActionStatus.NotFound, null);
        }

        var target = status is EquipmentStatus.AVAILABLE or EquipmentStatus.IN_USE
            ? await EquipmentStateMachine.GetRestingStatusAsync(_context, id)
            : status;

        if (!EquipmentStateMachine.TryApply(equipment, target, EquipmentTransition.Manual))
        {
            return new EquipmentResult(EquipmentActionStatus.InvalidStatusChange, null);
        }

        await _context.SaveChangesAsync();
        return new EquipmentResult(EquipmentActionStatus.Success, Map(equipment));
    }

    public async Task<EquipmentActionStatus> DeleteAsync(Guid id)
    {
        var equipment = await _context.Equipments
            .Include(e => e.Assignments)
            .Include(e => e.Maintenances)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (equipment is null)
        {
            return EquipmentActionStatus.NotFound;
        }

        if (equipment.Status == EquipmentStatus.DECOMMISSIONED)
        {
            return EquipmentActionStatus.AlreadyDecommissioned;
        }

        if (equipment.Ownership == EquipmentOwnership.RENTED)
        {
            return EquipmentActionStatus.RentedCannotBeDecommissioned;
        }

        // No se puede dar de baja un equipo que está en manos de alguien.
        var hasActiveAssignment = equipment.Assignments?
            .Any(a => a.Status == AssignmentStatus.ACTIVE) == true;
        if (hasActiveAssignment)
        {
            return EquipmentActionStatus.HasActiveAssignment;
        }

        // No se puede dar de baja un equipo que está en taller.
        var hasOpenMaintenance = equipment.Maintenances?
            .Any(m => m.Status == MaintenanceStatus.OPEN || m.Status == MaintenanceStatus.IN_PROGRESS) == true;
        if (hasOpenMaintenance)
        {
            return EquipmentActionStatus.HasOpenMaintenance;
        }

        if (!EquipmentStateMachine.TryApply(equipment, EquipmentStatus.DECOMMISSIONED, EquipmentTransition.Decommission))
        {
            return EquipmentActionStatus.InvalidStatusChange;
        }

        await _context.SaveChangesAsync();
        return EquipmentActionStatus.Success;
    }

    private async Task<List<string>> GetLockedFieldsAsync(Equipment equipment)
    {
        if (equipment.ExternalSourceId is not { } sourceId)
        {
            return new List<string>();
        }

        var mapping = await _context.ExternalSources.AsNoTracking()
            .Where(s => s.Id == sourceId && s.Active).Select(s => s.MappingJson).FirstOrDefaultAsync();
        return mapping is null
            ? new List<string>()
            : (JsonSerializer.Deserialize<Dictionary<string, string>>(mapping) ?? new()).Keys.ToList();
    }

    public async Task<EquipmentResponse?> GetByIdWithSourceAsync(Guid id, Guid currentUserId, string? role)
    {
        var response = await GetByIdAsync(id, currentUserId, role);
        if (response?.ExternalSourceId is { } sourceId)
        {
            var mapping = await _context.ExternalSources.AsNoTracking()
                .Where(s => s.Id == sourceId && s.Active).Select(s => s.MappingJson).FirstOrDefaultAsync();
            response.LockedFields = mapping is null
                ? new List<string>()
                : (JsonSerializer.Deserialize<Dictionary<string, string>>(mapping) ?? new()).Keys.ToList();
        }

        return response;
    }

    private sealed record RentalResolution(RentalContract? Contract, EquipmentActionStatus? Error);

    // Valida el contrato de un equipo alquilado: debe existir (en la organización), estar activo
    // y tener tarifa para el tipo de equipo. Al editar se acepta el contrato actual aunque ya haya terminado.
    private async Task<RentalResolution> ResolveRentalAsync(EquipmentOwnership ownership, Guid? contractId, string type,
        Guid? keepContractId = null)
    {
        if (ownership == EquipmentOwnership.OWNED)
        {
            return new RentalResolution(null, null);
        }

        if (contractId is null)
        {
            return new RentalResolution(null, EquipmentActionStatus.InvalidRentalContract);
        }

        var contract = await _context.RentalContracts.Include(c => c.Rates).Include(c => c.Lessor)
            .FirstOrDefaultAsync(c => c.Id == contractId);
        var typeHasRate = contract?.Rates?.Any(r => string.Equals(r.EquipmentType, type.Trim(), StringComparison.OrdinalIgnoreCase)) == true;
        var usable = contract is not null
            && (contract.Status == RentalContractStatus.ACTIVE || contract.Id == keepContractId)
            && typeHasRate;

        return usable
            ? new RentalResolution(contract, null)
            : new RentalResolution(null, EquipmentActionStatus.InvalidRentalContract);
    }

    private static EquipmentActionStatus? ValidateDates(DateOnly? acquisitionDate, DateOnly? warrantyUntil)
    {
        if (acquisitionDate is not null && acquisitionDate > LocalClock.Today)
        {
            return EquipmentActionStatus.FutureAcquisitionDate;
        }

        return DatesAreConsistent(acquisitionDate, warrantyUntil) ? null : EquipmentActionStatus.InvalidDates;
    }

    // Código y serie se guardan sin espacios extremos y en mayúsculas, para que "lap-01" y "LAP-01"
    // se reconozcan como el mismo equipo.
    private static string NormalizeIdentifier(string value) => value.Trim().ToUpperInvariant();

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool DatesAreConsistent(DateOnly? acquisitionDate, DateOnly? warrantyUntil)
    {
        return acquisitionDate is null || warrantyUntil is null || warrantyUntil >= acquisitionDate;
    }

    // Traduce la violación de los índices únicos (código interno, serie) a un resultado controlado,
    // incluso si dos peticiones simultáneas pasaron la verificación previa.
    private async Task<EquipmentActionStatus?> SaveDetectingDuplicatesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, "IX_Equipments_OrganizationId_InternalCode"))
        {
            return EquipmentActionStatus.DuplicateInternalCode;
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, "IX_Equipments_OrganizationId_SerialNumber"))
        {
            return EquipmentActionStatus.DuplicateSerialNumber;
        }
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
            OperatingSystem = equipment.OperatingSystem,
            AcquisitionDate = equipment.AcquisitionDate,
            AcquisitionPrice = equipment.AcquisitionPrice,
            WarrantyUntil = equipment.WarrantyUntil,
            Location = equipment.Location,
            Status = equipment.Status,
            Ownership = equipment.Ownership,
            RentalContractId = equipment.RentalContractId,
            ContractNumber = equipment.RentalContract?.ContractNumber,
            LessorName = equipment.RentalContract?.Lessor?.Name,
            AllowsInternalPreventive = equipment.RentalContract?.AllowsInternalPreventive ?? false,
            ReplacesEquipmentId = equipment.ReplacesEquipmentId,
            ReturnedAt = equipment.ReturnedAt,
            ExternalSourceId = equipment.ExternalSourceId,
            ExternalId = equipment.ExternalId,
            ExternalAttributes = ParseCharacteristics(equipment.ExternalAttributes),
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
