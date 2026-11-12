using Backend.API.Data;
using Hangfire;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

// Campana de notificaciones del usuario actual.
[Authorize(Roles = Roles.AnyRole)]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<object>> Mine()
    {
        var userId = User.Id();
        var items = await _context.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(30)
            .ToListAsync();
        var unread = await _context.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null);
        return Ok(new
        {
            unread,
            items = items.Select(n => new { n.Id, n.Title, n.Body, n.Link, n.Type, n.CreatedAt, n.ReadAt })
        });
    }

    [HttpPost("{id:guid}/Read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = User.Id();
        await _context.Notifications.Where(n => n.Id == id && n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow));
        return NoContent();
    }

    // Revisa los recordatorios ya (sin esperar la pasada de cada hora). Cada aviso se envía una sola vez.
    [Authorize(Roles = Roles.Administrador)]
    [HttpPost("RunReminders")]
    public IActionResult RunReminders([FromServices] IBackgroundJobClient jobs)
    {
        jobs.Enqueue<Backend.API.Services.ReminderJob>(job => job.RunAsync());
        return Accepted(new { message = "Revisión de recordatorios en curso." });
    }

    [HttpPost("ReadAll")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.Id();
        await _context.Notifications.Where(n => n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow));
        return NoContent();
    }
}
