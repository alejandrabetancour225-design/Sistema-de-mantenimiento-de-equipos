using System.ComponentModel.DataAnnotations;
using Backend.API.Validation;

namespace Backend.API.DTOs;

// Cualquier usuario autenticado cambia su propia contraseña indicando la actual.
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    [StringLength(StrongPasswordAttribute.MaxLength)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [StrongPassword]
    public string NewPassword { get; set; } = string.Empty;
}

public enum ChangePasswordStatus
{
    Success,
    UserNotFound,
    InvalidCurrentPassword,
    SameAsCurrent
}

public record ChangePasswordResult(ChangePasswordStatus Status, AuthResponse? Session);
