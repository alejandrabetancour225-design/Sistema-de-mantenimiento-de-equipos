using System.ComponentModel.DataAnnotations;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class UpdateMaintenanceRequest
{
    // Solo OPEN, IN_PROGRESS o CANCELLED. Para completar se usa CloseMaintenance.
    [EnumDataType(typeof(MaintenanceStatus))]
    public MaintenanceStatus? Status { get; set; }

    [StringLength(2000, MinimumLength = 3)]
    public string? ReportedProblem { get; set; }

    [StringLength(4000)]
    public string? WorkDone { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "La mano de obra debe estar entre 0 y 999.999.999.")]
    public decimal? LaborCost { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Los otros costos deben estar entre 0 y 999.999.999.")]
    public decimal? OtherCosts { get; set; }

    public DateOnly? NextMaintenanceDate { get; set; }

    [StringLength(2000)]
    public string? Observations { get; set; }

    public Guid? TechnicianId { get; set; }
}
