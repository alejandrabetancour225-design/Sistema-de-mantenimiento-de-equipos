using System.ComponentModel.DataAnnotations;
using Backend.API.Validation;

namespace Backend.API.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StrongPassword]
    public string Password { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no es válido.")]
    [StringLength(30)]
    public string? Phone { get; set; }
}
