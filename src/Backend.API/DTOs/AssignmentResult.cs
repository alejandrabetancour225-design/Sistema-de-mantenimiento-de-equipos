namespace Backend.API.DTOs;

public enum AssignmentActionStatus
{
    Success,
    NotFound,
    EquipmentNotFound,
    UserNotFound,
    EquipmentAlreadyAssigned,
    NotActive,
    // Equipo en mantenimiento, fuera de servicio o dado de baja.
    EquipmentNotAssignable,
    UserInactive,
    // El destinatario es Cliente/Gerente: solo se asigna a personal interno.
    UserNotAssignable
}

public record AssignmentResult(AssignmentActionStatus Status, AssignmentResponse? Assignment);
