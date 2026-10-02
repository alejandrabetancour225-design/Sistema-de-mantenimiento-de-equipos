namespace Backend.API.DTOs;

public enum SparePartActionStatus
{
    Success,
    NotFound,
    DuplicateName
}

public record SparePartResult(SparePartActionStatus Status, SparePartResponse? SparePart);
