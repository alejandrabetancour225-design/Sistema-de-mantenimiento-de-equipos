namespace Backend.API.DTOs;

public enum EquipmentActionStatus
{
    Success,
    NotFound,
    DuplicateInternalCode,
    DuplicateSerialNumber,
    // La garantía termina antes de la fecha de compra.
    InvalidDates,
    // UNDER_MAINTENANCE lo asigna solo el flujo de mantenimiento; tampoco se cambia el estado con un mantenimiento abierto.
    InvalidStatusChange,
    // Tiene asignaciones, componentes, incidentes o mantenimientos: dar de baja en lugar de borrar.
    HasRelatedRecords
}

public record EquipmentResult(EquipmentActionStatus Status, EquipmentResponse? Equipment);
