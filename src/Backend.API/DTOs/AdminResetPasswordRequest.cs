using System.ComponentModel.DataAnnotations;
using Backend.API.Validation;

namespace Backend.API.DTOs;

// El Administrador pone una contraseña temporal a un usuario de su organización.
public class AdminResetPasswordRequest
{
    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [StrongPassword]
    public string NewPassword { get; set; } = string.Empty;
}
