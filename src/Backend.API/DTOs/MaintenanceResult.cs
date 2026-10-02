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
    MaintenanceSparePartNotFound
}

public record MaintenanceResult(MaintenanceActionStatus Status, MaintenanceResponse? Maintenance);
