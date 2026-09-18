namespace Backend.API.DTOs;

public enum EquipmentActionStatus
{
    Success,
    NotFound,
    DuplicateInternalCode,
    DuplicateSerialNumber
}

public record EquipmentResult(EquipmentActionStatus Status, EquipmentResponse? Equipment);