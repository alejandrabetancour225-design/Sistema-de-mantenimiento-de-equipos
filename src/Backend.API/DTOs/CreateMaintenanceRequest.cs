using System.ComponentModel.DataAnnotations;
using Backend.API.Models;
using Backend.API.Validation;

namespace Backend.API.DTOs;

public class CreateMaintenanceRequest
{
    [Required]
    public Guid EquipmentId { get; set; }

    public Guid? IncidentId { get; set; }

    [Required]
    public Guid TechnicianId { get; set; }

    [EnumDataType(typeof(MaintenanceType))]
    public MaintenanceType Type { get; set; }

    [Required(ErrorMessage = "El problema reportado es obligatorio.")]
    [StringLength(2000, MinimumLength = 3)]
    public string ReportedProblem { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "La mano de obra debe estar entre 0 y 999.999.999.")]
    public decimal LaborCost { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Los otros costos deben estar entre 0 y 999.999.999.")]
    public decimal OtherCosts { get; set; }

    public DateOnly? NextMaintenanceDate { get; set; }

    [StringLength(2000)]
    public string Observations { get; set; } = string.Empty;

    [NotInFuture]
    public DateTime? StartedAt { get; set; }
}
