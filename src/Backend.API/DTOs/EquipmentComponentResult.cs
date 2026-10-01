namespace Backend.API.DTOs;

public enum EquipmentComponentActionStatus
{
    Success,
    NotFound,
    EquipmentNotFound
}

public record EquipmentComponentResult(EquipmentComponentActionStatus Status, EquipmentComponentResponse? Component);