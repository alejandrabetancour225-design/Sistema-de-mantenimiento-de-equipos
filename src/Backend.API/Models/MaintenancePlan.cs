namespace Backend.API.Models;

// Plan de mantenimiento preventivo por equipo (Q11): se repite cada N semanas.
// Al cerrar un mantenimiento preventivo del plan, la próxima fecha se calcula sola.
public class MaintenancePlan : IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid? TechnicianId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int IntervalWeeks { get; set; }
    public DateOnly NextDate { get; set; }
    public bool Active { get; set; } = true;
    public DateTime? LastCompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Equipment? Equipment { get; set; }
    public User? Technician { get; set; }
}

// Notificación interna (campana) para un usuario. También se envía por correo cuando aplica.
public class Notification : IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    // Ruta del frontend a la que lleva (p. ej. /calendario o /equipos/{id}).
    public string? Link { get; set; }
    public string Type { get; set; } = "general";
    // Evita repetir el mismo aviso (p. ej. "plan:{id}:{fecha}:7d").
    public string? DedupKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
