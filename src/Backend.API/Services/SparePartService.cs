using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Infrastructure;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class SparePartService : ISparePartService
{
    private readonly AppDbContext _context;

    public SparePartService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SparePartResponse>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.SpareParts.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(s => s.Active);
        }

        var spareParts = await query
            .OrderBy(s => s.Name)
            .ToListAsync();

        return spareParts.Select(Map).ToList();
    }

    public async Task<SparePartResponse?> GetByIdAsync(Guid id)
    {
        var sparePart = await _context.SpareParts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        return sparePart is null ? null : Map(sparePart);
    }

    public async Task<SparePartResult> CreateAsync(CreateSparePartRequest request)
    {
        var name = request.Name.Trim();
        if (await _context.SpareParts.AnyAsync(s => s.Name == name))
        {
            return new SparePartResult(SparePartActionStatus.DuplicateName, null);
        }

        var now = DateTime.UtcNow;
        var sparePart = new SparePart
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            UnitCost = request.UnitCost,
            Active = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.SpareParts.Add(sparePart);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, "IX_SpareParts_Name"))
        {
            return new SparePartResult(SparePartActionStatus.DuplicateName, null);
        }

        return new SparePartResult(SparePartActionStatus.Success, Map(sparePart));
    }

    public async Task<SparePartResult> UpdateAsync(Guid id, UpdateSparePartRequest request)
    {
        var sparePart = await _context.SpareParts.FirstOrDefaultAsync(s => s.Id == id);
        if (sparePart is null)
        {
            return new SparePartResult(SparePartActionStatus.NotFound, null);
        }

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (await _context.SpareParts.AnyAsync(s => s.Name == name && s.Id != id))
            {
                return new SparePartResult(SparePartActionStatus.DuplicateName, null);
            }

            sparePart.Name = name;
        }

        if (request.Description is not null)
        {
            sparePart.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.UnitCost is not null) sparePart.UnitCost = request.UnitCost.Value;
        if (request.Active is not null) sparePart.Active = request.Active.Value;

        sparePart.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, "IX_SpareParts_Name"))
        {
            return new SparePartResult(SparePartActionStatus.DuplicateName, null);
        }

        return new SparePartResult(SparePartActionStatus.Success, Map(sparePart));
    }

    public async Task<SparePartActionStatus> DeactivateAsync(Guid id)
    {
        var sparePart = await _context.SpareParts.FirstOrDefaultAsync(s => s.Id == id);
        if (sparePart is null)
        {
            return SparePartActionStatus.NotFound;
        }

        sparePart.Active = false;
        sparePart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return SparePartActionStatus.Success;
    }

    public async Task<SparePartActionStatus> ReactivateAsync(Guid id)
    {
        var sparePart = await _context.SpareParts.FirstOrDefaultAsync(s => s.Id == id);
        if (sparePart is null)
        {
            return SparePartActionStatus.NotFound;
        }

        sparePart.Active = true;
        sparePart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return SparePartActionStatus.Success;
    }

    private static SparePartResponse Map(SparePart sparePart)
    {
        return new SparePartResponse
        {
            Id = sparePart.Id,
            Name = sparePart.Name,
            Description = sparePart.Description,
            UnitCost = sparePart.UnitCost,
            Active = sparePart.Active,
            CreatedAt = sparePart.CreatedAt,
            UpdatedAt = sparePart.UpdatedAt
        };
    }
}
