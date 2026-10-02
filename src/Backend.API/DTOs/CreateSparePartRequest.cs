namespace Backend.API.DTOs;

public class CreateSparePartRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitCost { get; set; }
}
