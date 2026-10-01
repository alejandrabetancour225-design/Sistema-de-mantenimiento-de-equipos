namespace Backend.API.DTOs;

public enum IncidentActionStatus
{
    Success,
    NotFound,
    EquipmentNotFound,
    EquipmentHasOpenIncident
}

public record IncidentResult(IncidentActionStatus Status, IncidentResponse? Incident);