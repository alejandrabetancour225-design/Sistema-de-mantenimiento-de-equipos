namespace Backend.API.DTOs;

public enum MaintenanceActionStatus
{
    Success,
    NotFound,
    EquipmentNotFound,
    TechnicianNotFound,
    TechnicianRoleRequired,
    IncidentNotFound,
    IncidentAlreadyLinked,
    InvalidStatusTransition,
    SparePartNotFound,
    SparePartInactive,
    DuplicateSparePartInMaintenance,
    MaintenanceSparePartNotFound,
    // La incidencia pertenece a otro equipo.
    IncidentEquipmentMismatch,
    // La incidencia ya está resuelta o cerrada.
    IncidentNotOpen,
    // El equipo está dado de baja.
    EquipmentDecommissioned,
    // El equipo ya tiene un mantenimiento abierto o en progreso.
    EquipmentHasOpenMaintenance,
    // Para completar un mantenimiento hay que usar CloseMaintenance.
    MustUseClose,
    // Estado final del equipo o de la incidencia no permitido al cerrar.
    InvalidFinalStatus,
    // Fechas incoherentes (próximo mantenimiento antes del inicio, etc.).
    InvalidDates,
    // Un mantenimiento completado forma parte del historial y no se borra.
    CannotDeleteCompleted
}

public record MaintenanceResult(MaintenanceActionStatus Status, MaintenanceResponse? Maintenance);
