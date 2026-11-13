namespace Backend.API.Models;

// Reporte ejecutivo generado por IA (Q30): cifras del periodo + información de mercado con fuentes.
// Se genera en segundo plano (en CPU tarda minutos). Solo redacta: no cambia datos (R5).
public class AiReport : IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid Id { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    // PENDING | DONE | FAILED
    public string Status { get; set; } = "PENDING";
    public bool Automatic { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public string? StatsJson { get; set; }
    public string? ContentJson { get; set; }
    public string? MarketJson { get; set; }
    public byte[]? Pdf { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
