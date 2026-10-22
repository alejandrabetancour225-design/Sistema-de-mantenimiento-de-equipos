using System.Security.Claims;
using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ErrorKnowledgeController : ControllerBase
{
    private const string AdminOrTechnician = Roles.Administrador + "," + Roles.Tecnico;
    private readonly IErrorKnowledgeService _service;

    public ErrorKnowledgeController(IErrorKnowledgeService service)
    {
        _service = service;
    }

    [HttpGet("GetKnowledgeList")]
    public async Task<ActionResult<List<ErrorKnowledgeResponse>>> GetAll([FromQuery] string? component, [FromQuery] string? symptom)
    {
        return Ok(await _service.GetAllAsync(component, symptom));
    }

    [HttpGet("GetKnowledgeById/{id:guid}")]
    public async Task<ActionResult<ErrorKnowledgeResponse>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [Authorize(Roles = AdminOrTechnician)]
    [HttpPost("PostNewKnowledge")]
    public async Task<ActionResult<ErrorKnowledgeResponse>> Create(CreateErrorKnowledgeRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.CreateAsync(request, userId);
        return ToActionResult(result);
    }

    [Authorize(Roles = AdminOrTechnician)]
    [HttpPut("PutKnowledge/{id:guid}")]
    public async Task<ActionResult<ErrorKnowledgeResponse>> Update(Guid id, UpdateErrorKnowledgeRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return ToActionResult(result);
    }

    [Authorize(Roles = AdminOrTechnician)]
    [HttpDelete("DeleteKnowledge/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var status = await _service.DeleteAsync(id);
        return status == ErrorKnowledgeActionStatus.NotFound ? NotFound() : NoContent();
    }

    private ActionResult<ErrorKnowledgeResponse> ToActionResult(ErrorKnowledgeResult result)
    {
        return result.Status switch
        {
            ErrorKnowledgeActionStatus.Success => Ok(result.Knowledge),
            ErrorKnowledgeActionStatus.NotFound => NotFound(new { message = "Entrada no encontrada." }),
            ErrorKnowledgeActionStatus.DuplicateSymptom => Conflict(new { message = "Ya existe una entrada activa con ese síntoma." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
