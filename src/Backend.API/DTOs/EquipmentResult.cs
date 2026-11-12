namespace Backend.API.DTOs;

public enum EquipmentActionStatus
{
    Success,
    NotFound,
    DuplicateInternalCode,
    DuplicateSerialNumber,
    // La garantía termina antes de la fecha de compra.
    InvalidDates,
    // La fecha de adquisición es posterior a hoy.
    FutureAcquisitionDate,
    // El cambio de estado no está permitido por la regla maestra (R4).
    InvalidStatusChange,
    // Solo un Administrador puede cambiar el código interno o el número de serie.
    IdentityChangeForbidden,
    // Tiene asignaciones, componentes, incidentes o mantenimientos: dar de baja en lugar de borrar.
    HasRelatedRecords,
    AlreadyDecommissioned,
    HasActiveAssignment,
    HasOpenMaintenance
}

public record EquipmentResult(EquipmentActionStatus Status, EquipmentResponse? Equipment);
