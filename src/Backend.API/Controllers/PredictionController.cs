using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

[Authorize(Roles = Roles.Administrador + "," + Roles.Tecnico)]
[ApiController]
[Route("api/[controller]")]
public class PredictionController : ControllerBase
{
    private readonly IPredictionService _predictionService;

    public PredictionController(IPredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    [HttpGet("GetHighRisk")]
    public async Task<ActionResult<List<FailurePredictionResponse>>> GetHighRisk([FromQuery] int take = 10)
    {
        return Ok(await _predictionService.GetHighRiskAsync(take));
    }

    [HttpGet("GetByEquipment/{equipmentId:guid}")]
    public async Task<ActionResult<FailurePredictionResponse>> GetByEquipment(Guid equipmentId)
    {
        var prediction = await _predictionService.GetByEquipmentAsync(equipmentId);
        return prediction is null ? NotFound() : Ok(prediction);
    }

    [HttpPost("RefreshAll")]
    public async Task<ActionResult<object>> RefreshAll()
    {
        var count = await _predictionService.RefreshAllAsync();
        return Ok(new { message = $"Predicciones actualizadas: {count}", count });
    }

    [HttpPost("RefreshForEquipment/{equipmentId:guid}")]
    public async Task<ActionResult<FailurePredictionResponse>> RefreshForEquipment(Guid equipmentId)
    {
        var result = await _predictionService.RefreshForEquipmentAsync(equipmentId);
        return result.Status switch
        {
            PredictionActionStatus.Success => Ok(result.Prediction),
            PredictionActionStatus.EquipmentNotFound => NotFound(new { message = "El equipo no existe." }),
            _ => StatusCode(500)
        };
    }

    [HttpPost("Acknowledge/{predictionId:guid}")]
    public async Task<IActionResult> Acknowledge(Guid predictionId)
    {
        var success = await _predictionService.AcknowledgeAsync(predictionId);
        return success ? NoContent() : NotFound();
    }
}
