using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

// Cliente: sin acceso. Lectura: Administrador, Técnico y Empleado. Escritura: Administrador y Técnico.
[Authorize(Roles = Roles.Staff)]
[ApiController]
[Route("api/[controller]")]
public class EquipmentComponentController : ControllerBase
{
    private readonly IEquipmentComponentService _componentService;

    public EquipmentComponentController(IEquipmentComponentService componentService)
    {
        _componentService = componentService;
    }

    [HttpGet("GetComponentList")]
    public async Task<ActionResult<List<EquipmentComponentResponse>>> GetComponentList()
    {
        return Ok(await _componentService.GetAllAsync());
    }

    [HttpGet("GetComponentById/{id:guid}")]
    public async Task<ActionResult<EquipmentComponentResponse>> GetComponentById(Guid id)
    {
        var component = await _componentService.GetByIdAsync(id);
        return component is null ? NotFound() : Ok(component);
    }

    [HttpGet("GetComponentsByEquipment/{equipmentId:guid}")]
    public async Task<ActionResult<List<EquipmentComponentResponse>>> GetComponentsByEquipment(Guid equipmentId)
    {
        return Ok(await _componentService.GetByEquipmentAsync(equipmentId));
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPost("PostNewComponent")]
    public async Task<ActionResult<EquipmentComponentResponse>> PostNewComponent(CreateEquipmentComponentRequest request)
    {
        var result = await _componentService.CreateAsync(request);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpPut("PutComponent/{id:guid}")]
    public async Task<ActionResult<EquipmentComponentResponse>> PutComponent(Guid id, UpdateEquipmentComponentRequest request)
    {
        var result = await _componentService.UpdateAsync(id, request);
        return ToActionResult(result);
    }

    [Authorize(Roles = Roles.AdminOrTechnician)]
    [HttpDelete("DeleteComponent/{id:guid}")]
    public async Task<IActionResult> DeleteComponent(Guid id)
    {
        var status = await _componentService.DeleteAsync(id);
        return status == EquipmentComponentActionStatus.NotFound ? NotFound() : NoContent();
    }

    private ActionResult<EquipmentComponentResponse> ToActionResult(EquipmentComponentResult result)
    {
        return result.Status switch
        {
            EquipmentComponentActionStatus.Success => Ok(result.Component),
            EquipmentComponentActionStatus.NotFound => NotFound(new { message = "El componente no existe." }),
            EquipmentComponentActionStatus.EquipmentNotFound => NotFound(new { message = "El equipo no existe." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}