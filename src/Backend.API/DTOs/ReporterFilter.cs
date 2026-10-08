namespace Backend.API.DTOs;

public class ReportFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public Guid? AssignedTo { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? TechnicianId { get; set; }
}
