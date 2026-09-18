namespace Backend.API.DTOs;

public enum UpdateUserStatus
{
    Success,
    UserNotFound,
    RoleNotFound,
    EmailInUse
}

public record UpdateUserResult(UpdateUserStatus Status, UserResponse? User);