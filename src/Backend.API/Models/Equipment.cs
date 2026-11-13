namespace Backend.API.Models;

public class Equipment : IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid Id { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Characteristics { get; set; }
    public string? OperatingSystem { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public decimal? AcquisitionPrice { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public string Location { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.AVAILABLE;

    // Propio o alquilado. Si es alquilado pertenece a un contrato y su "fecha de adquisición"
    // es el inicio del contrato (Q9).
    public EquipmentOwnership Ownership { get; set; } = EquipmentOwnership.OWNED;
    public Guid? RentalContractId { get; set; }
    public RentalContract? RentalContract { get; set; }
    // Equipo de reemplazo enviado por el arrendador: apunta al equipo que reemplaza.
    public Guid? ReplacesEquipmentId { get; set; }
    public DateTime? ReturnedAt { get; set; }

    // Equipo importado de una base de terceros (Q27): sus campos mapeados se actualizan desde la
    // fuente y no se editan aquí. Las columnas sin equivalente se guardan en ExternalAttributes (JSONB).
    public Guid? ExternalSourceId { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalAttributes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Assignment>? Assignments { get; set; }
    public ICollection<EquipmentComponent>? Components { get; set; }
    public ICollection<Incident>? Incidents { get; set; }
    public ICollection<Maintenance>? Maintenances { get; set; }
}
