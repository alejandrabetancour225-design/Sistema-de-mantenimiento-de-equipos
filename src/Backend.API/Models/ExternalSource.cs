namespace Backend.API.Models;

// Fuente externa de datos de equipos de una empresa (Q3, Q26, Q27): archivo (CSV/Excel/JSON) o
// conexión en vivo de SOLO LECTURA a PostgreSQL, MySQL, SQL Server o MongoDB.
// El mapeo (campo nuestro -> columna de la fuente) lo propone python-service y lo confirma el Administrador.
public class ExternalSource : IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // file | postgres | mysql | sqlserver | mongodb
    public string Kind { get; set; } = "file";
    // Datos de conexión (JSON) cifrados con la clave de cifrado de la API. Nunca se devuelven.
    public string? EncryptedConnection { get; set; }
    public string? TableName { get; set; }
    public string? FileName { get; set; }
    public byte[]? FileContent { get; set; }
    // {"internalCode":"placa","serialNumber":"serie",...}
    public string MappingJson { get; set; } = "{}";
    public string KeyColumn { get; set; } = string.Empty;
    // 0 = solo manual. Si es mayor, la tarea programada sincroniza cada N horas.
    public int SyncIntervalHours { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncSummary { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
