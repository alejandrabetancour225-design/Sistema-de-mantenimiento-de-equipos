namespace Backend.API.DTOs;

public class UpdateSparePartRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? UnitCost { get; set; }
    public bool? Active { get; set; }
}
