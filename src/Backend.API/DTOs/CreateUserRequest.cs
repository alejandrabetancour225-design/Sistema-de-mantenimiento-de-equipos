using System.ComponentModel.DataAnnotations;
using Backend.API.Validation;

namespace Backend.API.DTOs;

// El Administrador crea los usuarios de su organización (reemplaza al registro público).
// La contraseña es temporal: el usuario debe cambiarla al iniciar sesión por primera vez.
public class CreateUserRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo inválido.")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña temporal es obligatoria.")]
    [StrongPassword]
    public string TemporaryPassword { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }
}
