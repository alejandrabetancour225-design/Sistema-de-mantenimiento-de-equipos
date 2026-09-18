using Backend.API.Data;
using Backend.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _context;

    public RoleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoleResponse>> GetRolesAsync()
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleResponse
            {
                Id = r.Id,
                Name = r.Name
            })
            .ToListAsync();
    }
}