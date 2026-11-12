namespace Backend.API.Models;

public class Assignment : IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.ACTIVE;
    public string Observations { get; set; } = string.Empty;

    public Equipment? Equipment { get; set; }
    public User? User { get; set; }
}