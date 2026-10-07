using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;

namespace Backend.API.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Record(string action, string entityType, string? entityId, string? details = null, Guid? userId = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            UserId = userId ?? AuditActor.GetUserId(httpContext),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = AuditActor.GetIpAddress(httpContext)
        });
    }
}
