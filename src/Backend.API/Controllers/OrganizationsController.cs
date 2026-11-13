using System.ComponentModel.DataAnnotations;
using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

public class CreateOrganizationRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [EnumDataType(typeof(MembershipPlan))]
    public MembershipPlan Plan { get; set; } = MembershipPlan.BASICO;

    // Primer Administrador de la organización.
    [Required]
    public CreateUserRequest Administrator { get; set; } = new();
}

public class UpdateOrganizationRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [EnumDataType(typeof(MembershipPlan))]
    public MembershipPlan Plan { get; set; }

    public bool Active { get; set; } = true;
}

// Gestión de la plataforma (solo SuperAdmin): organizaciones y su plan de membresía.
// Sin pagos ni cobros: el plan se asigna a mano (Q5).
[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUserService _users;

    public OrganizationsController(AppDbContext context, IUserService users)
    {
        _context = context;
        _users = users;
    }

    // Organización del usuario actual (para que el frontend sepa su plan).
    [Authorize(Roles = Roles.AnyRole)]
    [HttpGet("Current")]
    public async Task<ActionResult<object>> Current()
    {
        var orgId = Guid.Parse(User.FindFirst(AppClaimsOrg.Organization)!.Value);
        var org = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orgId);
        return org is null ? NotFound() : Ok(new { org.Id, org.Name, Plan = org.Plan.ToString(), PlanName = PlanNames.Display(org.Plan) });
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpGet]
    public async Task<ActionResult<object>> GetAll()
    {
        var orgs = await _context.Organizations.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
        // El SuperAdmin no ve datos de las empresas, solo cuántos usuarios y equipos tienen.
        var users = await _context.Users.IgnoreQueryFilters().Where(u => u.OrganizationId != null)
            .GroupBy(u => u.OrganizationId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync();
        var equipment = await _context.Equipments.IgnoreQueryFilters()
            .GroupBy(e => e.OrganizationId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync();

        return Ok(orgs.Select(o => new
        {
            o.Id,
            o.Name,
            Plan = o.Plan.ToString(),
            o.Active,
            o.CreatedAt,
            Users = users.FirstOrDefault(u => u.Id == o.Id)?.Count ?? 0,
            Equipment = equipment.FirstOrDefault(e => e.Id == o.Id)?.Count ?? 0
        }));
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpPost]
    public async Task<ActionResult<object>> Create(CreateOrganizationRequest request)
    {
        var name = request.Name.Trim();
        if (await _context.Organizations.AnyAsync(o => o.Name.ToLower() == name.ToLower()))
        {
            return Conflict(new { message = "Ya existe una organización con ese nombre." });
        }

        if (request.Administrator.Role != Roles.Administrador)
        {
            request.Administrator.Role = Roles.Administrador;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        var org = new Organization { Id = Guid.NewGuid(), Name = name, Plan = request.Plan, Active = true, CreatedAt = now, UpdatedAt = now };
        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        var admin = await _users.CreateUserAsync(request.Administrator, org.Id);
        if (admin.Status != UpdateUserStatus.Success)
        {
            await transaction.RollbackAsync();
            return admin.Status == UpdateUserStatus.EmailInUse
                ? Conflict(new { message = "El correo del administrador ya está registrado." })
                : BadRequest(new { message = "No se pudo crear el administrador de la organización." });
        }

        await transaction.CommitAsync();
        return Ok(new { org.Id, org.Name, Plan = org.Plan.ToString(), org.Active, Administrator = admin.User });
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<object>> Update(Guid id, UpdateOrganizationRequest request)
    {
        var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (org is null)
        {
            return NotFound(new { message = "La organización no existe." });
        }

        var name = request.Name.Trim();
        if (await _context.Organizations.AnyAsync(o => o.Id != id && o.Name.ToLower() == name.ToLower()))
        {
            return Conflict(new { message = "Ya existe una organización con ese nombre." });
        }

        org.Name = name;
        org.Plan = request.Plan;
        org.Active = request.Active;
        org.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { org.Id, org.Name, Plan = org.Plan.ToString(), org.Active });
    }
}
