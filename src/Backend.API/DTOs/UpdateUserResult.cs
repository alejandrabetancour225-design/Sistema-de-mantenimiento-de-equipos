namespace Backend.API.DTOs;

public enum UpdateUserStatus
{
    Success,
    UserNotFound,
    RoleNotFound,
    EmailInUse,
    // Dejaría al sistema sin ningún Administrador activo.
    LastAdministrator,
    // Un Administrador no puede desactivarse ni quitarse el rol a sí mismo.
    CannotModifySelf
}

public record UpdateUserResult(UpdateUserStatus Status, UserResponse? User);
