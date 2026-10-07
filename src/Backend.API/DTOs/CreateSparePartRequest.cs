using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class CreateSparePartRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "El costo unitario debe estar entre 0 y 999.999.999.")]
    public decimal UnitCost { get; set; }
}
