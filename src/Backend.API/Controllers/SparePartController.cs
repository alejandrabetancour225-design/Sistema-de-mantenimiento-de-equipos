using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SparePartController : ControllerBase
{
    private const string AdminOrTechnician = Roles.Administrador + "," + Roles.Tecnico;

    private readonly ISparePartService _sparePartService;

    public SparePartController(ISparePartService sparePartService)
    {
        _sparePartService = sparePartService;
    }

    [HttpGet("GetSparePartList")]
    public async Task<ActionResult<List<SparePartResponse>>> GetSparePartList([FromQuery] bool includeInactive = false)
    {
        return Ok(await _sparePartService.GetAllAsync(includeInactive));
    }

    [HttpGet("GetSparePartById/{id:guid}")]
    public async Task<ActionResult<SparePartResponse>> GetSparePartById(Guid id)
    {
        var sparePart = await _sparePartService.GetByIdAsync(id);
        return sparePart is null ? NotFound() : Ok(sparePart);
    }

    [Authorize(Roles = AdminOrTechnician)]
    [HttpPost("PostNewSparePart")]
    public async Task<ActionResult<SparePartResponse>> PostNewSparePart(CreateSparePartRequest request)
    {
        var result = await _sparePartService.CreateAsync(request);
        return ToActionResult(result);
    }

    [Authorize(Roles = AdminOrTechnician)]
    [HttpPut("PutSparePart/{id:guid}")]
    public async Task<ActionResult<SparePartResponse>> PutSparePart(Guid id, UpdateSparePartRequest request)
    {
        var result = await _sparePartService.UpdateAsync(id, request);
        return ToActionResult(result);
    }

    [Authorize(Roles = AdminOrTechnician)]
    [HttpPatch("DeactivateSparePart/{id:guid}")]
    public async Task<IActionResult> DeactivateSparePart(Guid id)
    {
        var status = await _sparePartService.DeactivateAsync(id);
        return status == SparePartActionStatus.NotFound ? NotFound() : NoContent();
    }

    [Authorize(Roles = AdminOrTechnician)]
    [HttpPatch("ReactivateSparePart/{id:guid}")]
    public async Task<IActionResult> ReactivateSparePart(Guid id)
    {
        var status = await _sparePartService.ReactivateAsync(id);
        return status == SparePartActionStatus.NotFound ? NotFound() : NoContent();
    }

    private ActionResult<SparePartResponse> ToActionResult(SparePartResult result)
    {
        return result.Status switch
        {
            SparePartActionStatus.Success => Ok(result.SparePart),
            SparePartActionStatus.NotFound => NotFound(new { message = "El repuesto no existe." }),
            SparePartActionStatus.DuplicateName => Conflict(
                new { message = "Ya existe un repuesto con ese nombre." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
