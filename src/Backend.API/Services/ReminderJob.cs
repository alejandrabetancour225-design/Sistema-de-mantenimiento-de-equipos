using Backend.API.Data;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

// Tarea programada (Hangfire, cada hora). Corre sin usuario: ve todas las organizaciones y
// envía cada aviso una sola vez gracias a la clave DedupKey de la notificación.
//  - Planes preventivos: aviso 7 días antes, 1 día antes y al vencer sin cerrarse (Q32).
//    Destinatarios: técnico del plan, administradores y el empleado que tiene el equipo.
//  - Contratos de alquiler: aviso 30 días antes del vencimiento a los administradores (Q31).
// Todo aviso va a la campana (SignalR) y por correo.
public class ReminderJob
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<ReminderJob> _logger;

    public ReminderJob(AppDbContext context, INotificationService notifications, ILogger<ReminderJob> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var today = LocalClock.Today;
        var sent = 0;

        var admins = await _context.Users.AsNoTracking()
            .Where(u => u.Active && u.OrganizationId != null && u.Role != null && u.Role.Name == Roles.Administrador)
            .Select(u => new { u.Id, u.OrganizationId }).ToListAsync();

        var plans = await _context.MaintenancePlans.AsNoTracking().Include(p => p.Equipment)
            .Where(p => p.Active && p.NextDate <= today.AddDays(7)).ToListAsync();
        foreach (var plan in plans)
        {
            var days = plan.NextDate.DayNumber - today.DayNumber;
            var (stage, when) = days switch
            {
                < 0 => ("vencido", $"está vencido desde el {plan.NextDate:dd/MM/yyyy}"),
                <= 1 => ("1d", days == 0 ? "es hoy" : "es mañana"),
                _ => ("7d", $"es el {plan.NextDate:dd/MM/yyyy} (en {days} días)")
            };

            var hasOpenMaintenance = await _context.Maintenances.AnyAsync(m => m.EquipmentId == plan.EquipmentId
                && (m.Status == MaintenanceStatus.OPEN || m.Status == MaintenanceStatus.IN_PROGRESS));
            if (stage == "vencido" && hasOpenMaintenance) continue; // ya se está atendiendo

            var holder = await _context.Assignments.AsNoTracking()
                .Where(a => a.EquipmentId == plan.EquipmentId && a.Status == AssignmentStatus.ACTIVE)
                .Select(a => (Guid?)a.UserId).FirstOrDefaultAsync();
            var recipients = admins.Where(a => a.OrganizationId == plan.OrganizationId).Select(a => a.Id).ToList();
            if (plan.TechnicianId is { } tech) recipients.Add(tech);
            if (holder is { } employee) recipients.Add(employee);

            var code = plan.Equipment?.InternalCode;
            sent += await _notifications.NotifyAsync(new NotificationRequest(
                plan.OrganizationId, recipients,
                stage == "vencido" ? $"Mantenimiento vencido: {code}" : $"Mantenimiento programado: {code}",
                $"El mantenimiento preventivo de {code} ({plan.Description}) {when}.",
                "/calendario", "mantenimiento",
                DedupKey: $"plan:{plan.Id}:{plan.NextDate:yyyyMMdd}:{stage}",
                SendEmail: true));
        }

        var contracts = await _context.RentalContracts
            .Include(c => c.Lessor)
            .Where(c => c.Status == RentalContractStatus.ACTIVE && c.ExpiryNotifiedAt == null && c.EndDate <= today.AddDays(30))
            .ToListAsync();
        foreach (var contract in contracts)
        {
            var days = contract.EndDate.DayNumber - today.DayNumber;
            sent += await _notifications.NotifyAsync(new NotificationRequest(
                contract.OrganizationId,
                admins.Where(a => a.OrganizationId == contract.OrganizationId).Select(a => a.Id).ToList(),
                $"Contrato de alquiler por vencer: {contract.ContractNumber}",
                days >= 0
                    ? $"El contrato {contract.ContractNumber} con {contract.Lessor?.Name} vence el {contract.EndDate:dd/MM/yyyy} (en {days} días). Decide si se renueva o se devuelven los equipos."
                    : $"El contrato {contract.ContractNumber} con {contract.Lessor?.Name} venció el {contract.EndDate:dd/MM/yyyy}.",
                "/alquiler", "alquiler",
                DedupKey: $"contract:{contract.Id}:{contract.EndDate:yyyyMMdd}",
                SendEmail: true));
            contract.ExpiryNotifiedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Recordatorios: {Count} notificaciones nuevas.", sent);
    }
}
