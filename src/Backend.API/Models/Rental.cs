namespace Backend.API.Models;

// Alquiler de equipos (Q8, Q31): la empresa cliente toma equipos en alquiler de un arrendador.

public enum EquipmentOwnership
{
    OWNED,
    RENTED
}

public enum RentalContractStatus
{
    ACTIVE,
    ENDED,
    CANCELLED
}

public enum LessorClaimStatus
{
    OPEN,
    ANSWERED,
    RESOLVED
}

// Empresa arrendadora (proveedora de los equipos alquilados).
public class Lessor : IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<RentalContract>? Contracts { get; set; }
}

public class RentalContract : IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid Id { get; set; }
    public Guid LessorId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public RentalContractStatus Status { get; set; } = RentalContractStatus.ACTIVE;
    // Si el contrato permite que el técnico interno haga mantenimiento preventivo.
    public bool AllowsInternalPreventive { get; set; }
    public string? Notes { get; set; }
    // Último aviso de vencimiento enviado (fase 5: recordatorio 30 días antes).
    public DateTime? ExpiryNotifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Lessor? Lessor { get; set; }
    public ICollection<RentalRate>? Rates { get; set; }
    public ICollection<Equipment>? Equipment { get; set; }
}

// Tarifa mensual (COP) por tipo de equipo dentro de un contrato.
public class RentalRate
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string EquipmentType { get; set; } = string.Empty;
    public decimal MonthlyRate { get; set; }

    public RentalContract? Contract { get; set; }
}

// Reclamo al arrendador por una falla de un equipo alquilado.
public class LessorClaim : IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid ContractId { get; set; }
    public Guid? IncidentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public LessorClaimStatus Status { get; set; } = LessorClaimStatus.OPEN;
    public string? LessorResponse { get; set; }
    public Guid CreatedByUserId { get; set; }
    // Cuándo se envió el correo al arrendador (null si no hay SMTP configurado o falló).
    public DateTime? NotifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public Equipment? Equipment { get; set; }
    public RentalContract? Contract { get; set; }
}
