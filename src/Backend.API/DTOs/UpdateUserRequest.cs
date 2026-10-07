using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class UpdateUserRequest
{
    // Activar una cuenta también quita el bloqueo temporal por intentos fallidos.
    public bool? Active { get; set; }

    [StringLength(50)]
    public string? Role { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }
}
