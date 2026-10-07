using System.ComponentModel.DataAnnotations;

namespace Backend.API.DTOs;

public class UpdateSparePartRequest
{
    [StringLength(150, MinimumLength = 2)]
    public string? Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "El costo unitario debe estar entre 0 y 999.999.999.")]
    public decimal? UnitCost { get; set; }

    public bool? Active { get; set; }
}
