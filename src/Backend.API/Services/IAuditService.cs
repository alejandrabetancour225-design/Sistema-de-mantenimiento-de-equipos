namespace Backend.API.Services;

public interface IAuditService
{
    // Agrega un evento de auditoría al contexto; se guarda con el siguiente SaveChanges.
    void Record(string action, string entityType, string? entityId, string? details = null, Guid? userId = null);
}
