using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

[Authorize(Roles = Roles.Administrador)]
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private const int MaxTake = 500;

    private readonly AppDbContext _context;

    public AuditController(AppDbContext context)
    {
        _context = context;
    }

    // Últimos eventos de auditoría, filtrables por usuario, entidad o acción.
    [HttpGet]
    public async Task<ActionResult<List<AuditLogResponse>>> GetAuditLogs(
        [FromQuery] Guid? userId,
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] string? action,
        [FromQuery] int take = 100)
    {
        take = Math.Clamp(take, 1, MaxTake);

        var query = _context.AuditLogs.AsNoTracking();
        if (userId is not null) query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(a => a.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .Select(a => new AuditLogResponse
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                UserId = a.UserId,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Details = a.Details,
                IpAddress = a.IpAddress
            })
            .ToListAsync();

        return Ok(logs);
    }
}
