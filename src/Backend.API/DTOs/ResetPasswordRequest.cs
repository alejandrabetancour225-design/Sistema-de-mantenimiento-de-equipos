using System.ComponentModel.DataAnnotations;
using Backend.API.Validation;

namespace Backend.API.DTOs;

// Usado por el Administrador para restablecer la contraseña de un usuario
// (el flujo de "olvidé mi contraseña" pasa por el Administrador).
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [StrongPassword]
    public string NewPassword { get; set; } = string.Empty;
}
