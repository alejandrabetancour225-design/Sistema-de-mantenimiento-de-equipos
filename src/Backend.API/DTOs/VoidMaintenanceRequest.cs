using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class VoidMaintenanceRequest
{
    [Required(ErrorMessage = "Indica el motivo de la anulación.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "El motivo debe tener entre 10 y 1000 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}
