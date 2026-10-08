using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.API.Models;

public class Incident
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid ReportedBy { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.OPEN;

    public Equipment? Equipment { get; set; }
    public User? Reporter { get; set; }
    public Maintenance? Maintenance { get; set; }

    [NotMapped]
    public Guid? MaintenanceId => Maintenance?.Id;
}
