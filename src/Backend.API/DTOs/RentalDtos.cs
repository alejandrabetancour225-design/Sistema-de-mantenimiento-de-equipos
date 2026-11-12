using System.ComponentModel.DataAnnotations;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class LessorRequest
{
    [Required(ErrorMessage = "El nombre del arrendador es obligatorio.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(30)]
    public string? TaxId { get; set; }

    [Required(ErrorMessage = "El nombre del contacto es obligatorio.")]
    [StringLength(150, MinimumLength = 2)]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo del contacto es obligatorio (ahí llegan los reclamos).")]
    [EmailAddress(ErrorMessage = "Correo inválido.")]
    [StringLength(254)]
    public string ContactEmail { get; set; } = string.Empty;

    [StringLength(30)]
    public string? ContactPhone { get; set; }

    public bool Active { get; set; } = true;
}

public class RentalRateRequest
{
    [Required(ErrorMessage = "El tipo de equipo es obligatorio.")]
    [StringLength(100, MinimumLength = 1)]
    public string EquipmentType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "La tarifa mensual debe estar entre 0 y 999.999.999.")]
    public decimal MonthlyRate { get; set; }
}

public class RentalContractRequest
{
    [Required]
    public Guid LessorId { get; set; }

    [Required(ErrorMessage = "El número de contrato es obligatorio.")]
    [StringLength(60, MinimumLength = 1)]
    public string ContractNumber { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    public bool AllowsInternalPreventive { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [MinLength(1, ErrorMessage = "Define al menos una tarifa por tipo de equipo.")]
    public List<RentalRateRequest> Rates { get; set; } = new();
}

public class ReplacementRequest
{
    [Required(ErrorMessage = "El código interno es obligatorio.")]
    [StringLength(50, MinimumLength = 1)]
    public string InternalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de serie es obligatorio.")]
    [StringLength(100, MinimumLength = 1)]
    public string SerialNumber { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Brand { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Model { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }
}

public class CreateClaimRequest
{
    [Required]
    public Guid EquipmentId { get; set; }

    public Guid? IncidentId { get; set; }

    [Required(ErrorMessage = "Describe la falla para el arrendador.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres.")]
    public string Description { get; set; } = string.Empty;
}

public class UpdateClaimRequest
{
    [EnumDataType(typeof(LessorClaimStatus))]
    public LessorClaimStatus Status { get; set; }

    [StringLength(2000)]
    public string? LessorResponse { get; set; }
}
