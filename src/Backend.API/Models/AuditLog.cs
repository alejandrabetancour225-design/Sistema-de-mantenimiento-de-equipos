namespace Backend.API.Models;

// Registro de auditoría: quién hizo qué, sobre qué registro y cuándo.
// No guarda valores (para no duplicar datos personales), solo los nombres de los campos modificados.
public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}
