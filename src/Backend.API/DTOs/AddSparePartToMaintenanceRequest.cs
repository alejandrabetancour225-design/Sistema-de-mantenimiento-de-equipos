using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class AddSparePartToMaintenanceRequest
{
    [Required]
    public Guid SparePartId { get; set; }

    [Range(1, 10_000, ErrorMessage = "La cantidad debe estar entre 1 y 10.000.")]
    public int Quantity { get; set; }
}
