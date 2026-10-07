namespace Backend.API.DTOs;

public enum IncidentActionStatus
{
    Success,
    NotFound,
    EquipmentNotFound,
    EquipmentHasOpenIncident,
    // El equipo está dado de baja.
    EquipmentDecommissioned,
    // El estado pedido no está permitido para este rol o este incidente.
    InvalidStatusChange,
    // Tiene un mantenimiento asociado: no se puede borrar ni editar libremente.
    HasMaintenance
}

public record IncidentResult(IncidentActionStatus Status, IncidentResponse? Incident);
