using Backend.API.Models;

namespace Backend.API.DTOs;

public class UpdateIncidentRequest
{
    public string? Description { get; set; }
    public IncidentStatus? Status { get; set; }
    public Guid? MaintenanceId { get; set; }
}