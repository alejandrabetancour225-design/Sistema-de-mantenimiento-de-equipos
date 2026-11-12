using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Controllers;

// Equipos alquilados (Q8, Q31): arrendadores, contratos con tarifa mensual por tipo de equipo,
// devolución (de un equipo o de todo el contrato), equipos de reemplazo y reclamos al arrendador.
// Consulta: Administrador, Técnico y Gerente. Gestión: Administrador y Técnico. Devolver: Administrador.
[Authorize(Roles = Roles.Administrador + "," + Roles.Tecnico + "," + Roles.Gerente)]
[ApiController]
[Route("api/[controller]")]
public class RentalController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _email;
    private readonly ILogger<RentalController> _logger;

    public RentalController(AppDbContext context, IEmailSender email, ILogger<RentalController> logger)
    {
        _context = context;
        _email = email;
        _logger = logger;
    }

    // ---------------- Arrendadores ----------------

    [HttpGet("Lessors")]
    public async Task<ActionResult<object>> GetLessors()
        => Ok(await _context.Lessors.AsNoTracking().OrderBy(l => l.Name).ToListAsync());

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("Lessors")]
    public async Task<ActionResult<Lessor>> CreateLessor(LessorRequest request)
    {
        if (await _context.Lessors.AnyAsync(l => l.Name.ToLower() == request.Name.Trim().ToLower()))
        {
            return Conflict(new { message = "Ya existe un arrendador con ese nombre." });
        }

        var now = DateTime.UtcNow;
        var lessor = new Lessor { Id = Guid.NewGuid(), CreatedAt = now, UpdatedAt = now };
        Apply(lessor, request);
        _context.Lessors.Add(lessor);
        await _context.SaveChangesAsync();
        return Ok(lessor);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPut("Lessors/{id:guid}")]
    public async Task<ActionResult<Lessor>> UpdateLessor(Guid id, LessorRequest request)
    {
        var lessor = await _context.Lessors.FirstOrDefaultAsync(l => l.Id == id);
        if (lessor is null) return NotFound(new { message = "El arrendador no existe." });
        if (await _context.Lessors.AnyAsync(l => l.Id != id && l.Name.ToLower() == request.Name.Trim().ToLower()))
        {
            return Conflict(new { message = "Ya existe un arrendador con ese nombre." });
        }

        Apply(lessor, request);
        lessor.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(lessor);
    }

    private static void Apply(Lessor lessor, LessorRequest r)
    {
        lessor.Name = r.Name.Trim();
        lessor.TaxId = string.IsNullOrWhiteSpace(r.TaxId) ? null : r.TaxId.Trim();
        lessor.ContactName = r.ContactName.Trim();
        lessor.ContactEmail = r.ContactEmail.Trim();
        lessor.ContactPhone = string.IsNullOrWhiteSpace(r.ContactPhone) ? null : r.ContactPhone.Trim();
        lessor.Active = r.Active;
    }

    // ---------------- Contratos ----------------

    // Contratos con sus tarifas, equipos vigentes y costo mensual (suma de la tarifa de cada equipo no devuelto).
    [HttpGet("Contracts")]
    public async Task<ActionResult<object>> GetContracts()
    {
        var contracts = await _context.RentalContracts.AsNoTracking()
            .Include(c => c.Lessor)
            .Include(c => c.Rates)
            .Include(c => c.Equipment)
            .OrderBy(c => c.EndDate)
            .ToListAsync();

        var today = LocalClock.Today;
        return Ok(contracts.Select(c => MapContract(c, today)));
    }

    // Contratos activos que vencen en los próximos días (aviso de 30 días, Q31).
    [HttpGet("Contracts/Expiring")]
    public async Task<ActionResult<object>> GetExpiring([FromQuery] int days = 30)
    {
        var today = LocalClock.Today;
        var limit = today.AddDays(Math.Clamp(days, 1, 365));
        var contracts = await _context.RentalContracts.AsNoTracking()
            .Include(c => c.Lessor).Include(c => c.Rates).Include(c => c.Equipment)
            .Where(c => c.Status == RentalContractStatus.ACTIVE && c.EndDate <= limit)
            .OrderBy(c => c.EndDate)
            .ToListAsync();
        return Ok(contracts.Select(c => MapContract(c, today)));
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("Contracts")]
    public async Task<ActionResult<object>> CreateContract(RentalContractRequest request)
    {
        var error = await ValidateContractAsync(request, null);
        if (error is not null) return error;

        var now = DateTime.UtcNow;
        var contract = new RentalContract
        {
            Id = Guid.NewGuid(),
            LessorId = request.LessorId,
            ContractNumber = request.ContractNumber.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AllowsInternalPreventive = request.AllowsInternalPreventive,
            Notes = request.Notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Rates = request.Rates.Select(r => new RentalRate
            {
                Id = Guid.NewGuid(),
                EquipmentType = r.EquipmentType.Trim(),
                MonthlyRate = r.MonthlyRate
            }).ToList()
        };
        _context.RentalContracts.Add(contract);
        await _context.SaveChangesAsync();
        return Ok(new { contract.Id });
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPut("Contracts/{id:guid}")]
    public async Task<ActionResult<object>> UpdateContract(Guid id, RentalContractRequest request)
    {
        var contract = await _context.RentalContracts.Include(c => c.Rates).Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound(new { message = "El contrato no existe." });
        if (contract.Status != RentalContractStatus.ACTIVE)
        {
            return BadRequest(new { message = "Un contrato terminado no se modifica." });
        }

        var error = await ValidateContractAsync(request, id);
        if (error is not null) return error;

        // No se puede quitar la tarifa de un tipo que aún tiene equipos alquilados en el contrato.
        var typesInUse = contract.Equipment!.Where(e => e.Status != EquipmentStatus.RETURNED)
            .Select(e => e.Type.ToLowerInvariant()).Distinct();
        var newTypes = request.Rates.Select(r => r.EquipmentType.Trim().ToLowerInvariant()).ToHashSet();
        var missing = typesInUse.Where(t => !newTypes.Contains(t)).ToList();
        if (missing.Count > 0)
        {
            return BadRequest(new { message = $"Hay equipos alquilados de tipo {string.Join(", ", missing)}: conserva su tarifa." });
        }

        contract.LessorId = request.LessorId;
        contract.ContractNumber = request.ContractNumber.Trim();
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.AllowsInternalPreventive = request.AllowsInternalPreventive;
        contract.Notes = request.Notes?.Trim();
        contract.UpdatedAt = DateTime.UtcNow;
        _context.RentalRates.RemoveRange(contract.Rates!);
        _context.RentalRates.AddRange(request.Rates.Select(r => new RentalRate
        {
            Id = Guid.NewGuid(),
            ContractId = contract.Id,
            EquipmentType = r.EquipmentType.Trim(),
            MonthlyRate = r.MonthlyRate
        }));
        await _context.SaveChangesAsync();
        return Ok(new { contract.Id });
    }

    // Fin del contrato: devuelve todos los equipos que se pueden devolver. Los que siguen asignados
    // o en mantenimiento se informan y el contrato sigue activo hasta devolverlos.
    [Authorize(Roles = Roles.Administrador)]
    [HttpPost("Contracts/{id:guid}/ReturnAll")]
    public async Task<ActionResult<object>> ReturnAll(Guid id)
    {
        var contract = await _context.RentalContracts.Include(c => c.Equipment).FirstOrDefaultAsync(c => c.Id == id);
        if (contract is null) return NotFound(new { message = "El contrato no existe." });

        var returned = new List<string>();
        var pending = new List<string>();
        foreach (var equipment in contract.Equipment!.Where(e => e.Status != EquipmentStatus.RETURNED))
        {
            if (EquipmentStateMachine.TryApply(equipment, EquipmentStatus.RETURNED, EquipmentTransition.Return))
            {
                equipment.ReturnedAt = DateTime.UtcNow;
                returned.Add(equipment.InternalCode);
            }
            else
            {
                pending.Add($"{equipment.InternalCode} ({equipment.Status})");
            }
        }

        if (pending.Count == 0)
        {
            contract.Status = RentalContractStatus.ENDED;
            contract.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            returned,
            pending,
            contractStatus = contract.Status.ToString(),
            message = pending.Count == 0
                ? "Contrato terminado: todos los equipos quedaron devueltos."
                : "Algunos equipos no se pudieron devolver (libéralos o cierra su mantenimiento y repite)."
        });
    }

    // ---------------- Equipos alquilados ----------------

    // Devolver un equipo alquilado al arrendador (R4: Disponible o Fuera de servicio -> Devuelto).
    [Authorize(Roles = Roles.Administrador)]
    [HttpPost("Equipment/{equipmentId:guid}/Return")]
    public async Task<ActionResult<object>> ReturnEquipment(Guid equipmentId)
    {
        var equipment = await _context.Equipments.Include(e => e.RentalContract).FirstOrDefaultAsync(e => e.Id == equipmentId);
        if (equipment is null) return NotFound(new { message = "El equipo no existe." });
        if (equipment.Ownership != EquipmentOwnership.RENTED)
        {
            return BadRequest(new { message = "Solo los equipos alquilados se devuelven; un equipo propio se da de baja." });
        }

        if (equipment.Status == EquipmentStatus.RETURNED)
        {
            return Conflict(new { message = "El equipo ya fue devuelto al arrendador." });
        }

        if (!EquipmentStateMachine.TryApply(equipment, EquipmentStatus.RETURNED, EquipmentTransition.Return))
        {
            return Conflict(new { message = "Solo se devuelve un equipo Disponible o Fuera de servicio: libera su asignación o cierra su mantenimiento primero." });
        }

        equipment.ReturnedAt = DateTime.UtcNow;

        // Si ya no quedan equipos vigentes, el contrato termina.
        var contract = equipment.RentalContract!;
        var othersActive = await _context.Equipments.AnyAsync(e =>
            e.RentalContractId == contract.Id && e.Id != equipment.Id && e.Status != EquipmentStatus.RETURNED);
        if (!othersActive && contract.EndDate <= LocalClock.Today)
        {
            contract.Status = RentalContractStatus.ENDED;
        }

        await _context.SaveChangesAsync();
        return Ok(new { equipment.Id, Status = equipment.Status.ToString() });
    }

    // Registrar el equipo de reemplazo que envía el arrendador por uno devuelto.
    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("Equipment/{equipmentId:guid}/Replacement")]
    public async Task<ActionResult<object>> RegisterReplacement(Guid equipmentId, ReplacementRequest request)
    {
        var original = await _context.Equipments.Include(e => e.RentalContract)
            .FirstOrDefaultAsync(e => e.Id == equipmentId);
        if (original is null) return NotFound(new { message = "El equipo no existe." });
        if (original.Ownership != EquipmentOwnership.RENTED || original.Status != EquipmentStatus.RETURNED)
        {
            return BadRequest(new { message = "El reemplazo se registra sobre un equipo alquilado ya devuelto." });
        }

        if (original.RentalContract!.Status != RentalContractStatus.ACTIVE)
        {
            return BadRequest(new { message = "El contrato de ese equipo ya terminó." });
        }

        var code = request.InternalCode.Trim().ToUpperInvariant();
        var serial = request.SerialNumber.Trim().ToUpperInvariant();
        if (await _context.Equipments.AnyAsync(e => e.InternalCode == code || e.SerialNumber == serial))
        {
            return Conflict(new { message = "Ya existe un equipo con ese código o número de serie." });
        }

        var now = DateTime.UtcNow;
        var replacement = new Equipment
        {
            Id = Guid.NewGuid(),
            InternalCode = code,
            SerialNumber = serial,
            Type = original.Type,
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? original.Location : request.Location.Trim(),
            OperatingSystem = original.OperatingSystem,
            Ownership = EquipmentOwnership.RENTED,
            RentalContractId = original.RentalContractId,
            AcquisitionDate = LocalClock.Today,
            ReplacesEquipmentId = original.Id,
            Status = EquipmentStatus.AVAILABLE,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Equipments.Add(replacement);
        await _context.SaveChangesAsync();
        return Ok(new { replacement.Id, replacement.InternalCode });
    }

    // ---------------- Reclamos al arrendador ----------------

    [HttpGet("Claims")]
    public async Task<ActionResult<object>> GetClaims()
    {
        var claims = await _context.LessorClaims.AsNoTracking()
            .Include(c => c.Equipment)
            .Include(c => c.Contract).ThenInclude(c => c!.Lessor)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(claims.Select(c => new
        {
            c.Id,
            c.EquipmentId,
            EquipmentCode = c.Equipment?.InternalCode,
            c.ContractId,
            ContractNumber = c.Contract?.ContractNumber,
            LessorName = c.Contract?.Lessor?.Name,
            c.IncidentId,
            c.Description,
            Status = c.Status.ToString(),
            c.LessorResponse,
            c.NotifiedAt,
            c.CreatedAt,
            c.ResolvedAt
        }));
    }

    // Una falla en un equipo alquilado se comunica al arrendador (correo a su contacto).
    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("Claims")]
    public async Task<ActionResult<object>> CreateClaim(CreateClaimRequest request)
    {
        var equipment = await _context.Equipments.Include(e => e.RentalContract).ThenInclude(c => c!.Lessor)
            .FirstOrDefaultAsync(e => e.Id == request.EquipmentId);
        if (equipment is null) return NotFound(new { message = "El equipo no existe." });
        if (equipment.Ownership != EquipmentOwnership.RENTED || equipment.RentalContract is null)
        {
            return BadRequest(new { message = "Los reclamos al arrendador son solo para equipos alquilados." });
        }

        if (request.IncidentId is { } incidentId
            && !await _context.Incidents.AnyAsync(i => i.Id == incidentId && i.EquipmentId == equipment.Id))
        {
            return BadRequest(new { message = "La incidencia no pertenece a ese equipo." });
        }

        var now = DateTime.UtcNow;
        var claim = new LessorClaim
        {
            Id = Guid.NewGuid(),
            EquipmentId = equipment.Id,
            ContractId = equipment.RentalContract.Id,
            IncidentId = request.IncidentId,
            Description = request.Description.Trim(),
            CreatedByUserId = User.Id(),
            CreatedAt = now,
            UpdatedAt = now
        };

        var lessor = equipment.RentalContract.Lessor!;
        try
        {
            await _email.SendAsync(lessor.ContactEmail,
                $"Reclamo por falla - equipo {equipment.InternalCode} (contrato {equipment.RentalContract.ContractNumber})",
                $"Hola {lessor.ContactName},\n\nReportamos una falla en el equipo alquilado {equipment.InternalCode} "
                + $"({equipment.Type} {equipment.Brand} {equipment.Model}, serie {equipment.SerialNumber}), "
                + $"del contrato {equipment.RentalContract.ContractNumber}.\n\nDescripción: {claim.Description}\n\n"
                + "Quedamos atentos a su respuesta para la revisión o el reemplazo del equipo.\n");
            claim.NotifiedAt = now;
        }
        catch (Exception ex)
        {
            // El reclamo queda registrado aunque el correo falle; se puede reenviar después.
            _logger.LogWarning(ex, "No se pudo enviar el correo del reclamo {ClaimId}", claim.Id);
        }

        _context.LessorClaims.Add(claim);
        await _context.SaveChangesAsync();
        return Ok(new { claim.Id, claim.NotifiedAt, LessorEmail = lessor.ContactEmail });
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPut("Claims/{id:guid}")]
    public async Task<ActionResult<object>> UpdateClaim(Guid id, UpdateClaimRequest request)
    {
        var claim = await _context.LessorClaims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null) return NotFound(new { message = "El reclamo no existe." });
        if (claim.Status == LessorClaimStatus.RESOLVED)
        {
            return BadRequest(new { message = "El reclamo ya está resuelto." });
        }

        claim.Status = request.Status;
        claim.LessorResponse = string.IsNullOrWhiteSpace(request.LessorResponse) ? claim.LessorResponse : request.LessorResponse.Trim();
        claim.UpdatedAt = DateTime.UtcNow;
        if (request.Status == LessorClaimStatus.RESOLVED)
        {
            claim.ResolvedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { claim.Id, Status = claim.Status.ToString() });
    }

    // ---------------- Apoyo ----------------

    private async Task<ActionResult?> ValidateContractAsync(RentalContractRequest request, Guid? exceptId)
    {
        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new { message = "La fecha de fin del contrato no puede ser anterior al inicio." });
        }

        if (!await _context.Lessors.AnyAsync(l => l.Id == request.LessorId && l.Active))
        {
            return BadRequest(new { message = "El arrendador no existe o está inactivo." });
        }

        var number = request.ContractNumber.Trim();
        if (await _context.RentalContracts.AnyAsync(c => c.ContractNumber == number && c.Id != exceptId))
        {
            return Conflict(new { message = "Ya existe un contrato con ese número." });
        }

        var types = request.Rates.Select(r => r.EquipmentType.Trim().ToLowerInvariant()).ToList();
        if (types.Count != types.Distinct().Count())
        {
            return BadRequest(new { message = "Hay tipos de equipo repetidos en las tarifas." });
        }

        return null;
    }

    private static object MapContract(RentalContract c, DateOnly today)
    {
        var rates = c.Rates ?? new List<RentalRate>();
        var active = (c.Equipment ?? new List<Equipment>()).Where(e => e.Status != EquipmentStatus.RETURNED).ToList();
        var monthlyCost = active.Sum(e => rates.FirstOrDefault(r =>
            string.Equals(r.EquipmentType, e.Type, StringComparison.OrdinalIgnoreCase))?.MonthlyRate ?? 0m);
        return new
        {
            c.Id,
            c.LessorId,
            LessorName = c.Lessor?.Name,
            LessorEmail = c.Lessor?.ContactEmail,
            c.ContractNumber,
            c.StartDate,
            c.EndDate,
            DaysToExpiry = c.EndDate.DayNumber - today.DayNumber,
            Status = c.Status.ToString(),
            c.AllowsInternalPreventive,
            c.Notes,
            Rates = rates.OrderBy(r => r.EquipmentType).Select(r => new { r.EquipmentType, r.MonthlyRate }),
            ActiveEquipment = active.Count,
            ReturnedEquipment = (c.Equipment?.Count ?? 0) - active.Count,
            MonthlyCost = monthlyCost
        };
    }
}
