using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class AssignmentService : IAssignmentService
{
    private readonly AppDbContext _context;

    public AssignmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssignmentResponse>> GetAllAsync(Guid currentUserId, string? role)
    {
        var canSeeAll = role is Roles.Administrador or Roles.Tecnico;

        var query = _context.Assignments
            .AsNoTracking()
            .Include(a => a.Equipment)
            .Include(a => a.User)
            .AsQueryable();

        if (!canSeeAll)
        {
            query = query.Where(a => a.UserId == currentUserId);
        }

        var assignments = await query
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();

        return assignments.Select(Map).ToList();
    }

    public async Task<AssignmentResult> AssignAsync(CreateAssignmentRequest request)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == request.EquipmentId);
        if (equipment is null)
        {
            return new AssignmentResult(AssignmentActionStatus.EquipmentNotFound, null);
        }

        if (equipment.Status is EquipmentStatus.UNDER_MAINTENANCE
            or EquipmentStatus.OUT_OF_SERVICE
            or EquipmentStatus.DECOMMISSIONED)
        {
            return new AssignmentResult(AssignmentActionStatus.EquipmentNotAssignable, null);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
        if (user is null)
        {
            return new AssignmentResult(AssignmentActionStatus.UserNotFound, null);
        }

        if (!user.Active)
        {
            return new AssignmentResult(AssignmentActionStatus.UserInactive, null);
        }

        var alreadyAssigned = await _context.Assignments
            .AnyAsync(a => a.EquipmentId == request.EquipmentId && a.Status == AssignmentStatus.ACTIVE);
        if (alreadyAssigned)
        {
            return new AssignmentResult(AssignmentActionStatus.EquipmentAlreadyAssigned, null);
        }

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            EquipmentId = request.EquipmentId,
            UserId = request.UserId,
            AssignedAt = DateTime.UtcNow,
            ReleasedAt = null,
            Status = AssignmentStatus.ACTIVE,
            Observations = request.Observations?.Trim() ?? string.Empty
        };

        _context.Assignments.Add(assignment);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, AppDbContext.ActiveAssignmentPerEquipmentIndex))
        {
            // Otra petición asignó el mismo equipo al mismo tiempo: el índice único lo impidió.
            return new AssignmentResult(AssignmentActionStatus.EquipmentAlreadyAssigned, null);
        }

        await _context.Entry(assignment).Reference(a => a.Equipment).LoadAsync();
        await _context.Entry(assignment).Reference(a => a.User).LoadAsync();

        return new AssignmentResult(AssignmentActionStatus.Success, Map(assignment));
    }

    public async Task<AssignmentResult> ReleaseAsync(Guid id, string? observations)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Equipment)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment is null)
        {
            return new AssignmentResult(AssignmentActionStatus.NotFound, null);
        }

        if (assignment.Status != AssignmentStatus.ACTIVE)
        {
            return new AssignmentResult(AssignmentActionStatus.NotActive, null);
        }

        assignment.Status = AssignmentStatus.RELEASED;
        assignment.ReleasedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(observations))
        {
            assignment.Observations = observations.Trim();
        }

        await _context.SaveChangesAsync();

        return new AssignmentResult(AssignmentActionStatus.Success, Map(assignment));
    }

    private static AssignmentResponse Map(Assignment assignment)
    {
        return new AssignmentResponse
        {
            Id = assignment.Id,
            EquipmentId = assignment.EquipmentId,
            UserId = assignment.UserId,
            EquipmentInternalCode = assignment.Equipment?.InternalCode,
            EquipmentSerialNumber = assignment.Equipment?.SerialNumber,
            UserFullName = assignment.User?.FullName,
            AssignedAt = assignment.AssignedAt,
            ReleasedAt = assignment.ReleasedAt,
            Status = assignment.Status,
            Observations = assignment.Observations
        };
    }
}
