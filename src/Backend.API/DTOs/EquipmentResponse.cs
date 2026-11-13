using System.Text.Json;
using Backend.API.Models;

namespace Backend.API.DTOs;

public class EquipmentResponse
{
    public Guid Id { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public JsonElement? Characteristics { get; set; }
    public string? OperatingSystem { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public decimal? AcquisitionPrice { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public string Location { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; }
    public EquipmentOwnership Ownership { get; set; }
    public Guid? RentalContractId { get; set; }
    public string? ContractNumber { get; set; }
    public string? LessorName { get; set; }
    public bool AllowsInternalPreventive { get; set; }
    public Guid? ReplacesEquipmentId { get; set; }
    public DateTime? ReturnedAt { get; set; }
    // Importado de una base de terceros: nombre de la fuente, campos bloqueados y columnas extra.
    public Guid? ExternalSourceId { get; set; }
    public string? ExternalId { get; set; }
    public JsonElement? ExternalAttributes { get; set; }
    public List<string> LockedFields { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
