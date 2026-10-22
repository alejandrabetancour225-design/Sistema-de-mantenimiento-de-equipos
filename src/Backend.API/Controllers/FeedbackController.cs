using System.Security.Claims;
using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers;

[Authorize(Roles = Roles.Administrador + "," + Roles.Tecnico)]
[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost("CreateFeedback")]
    public async Task<ActionResult<FeedbackResponse>> CreateFeedback(CreateFeedbackRequest request)
    {
        var technicianId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _feedbackService.CreateAsync(request, technicianId);

        return result.Status switch
        {
            FeedbackActionStatus.Success => Ok(result.Feedback),
            FeedbackActionStatus.DiagnosisNotFound => NotFound(new { message = "El diagnóstico no existe." }),
            FeedbackActionStatus.DiagnosisNotPending => BadRequest(new { message = "Este diagnóstico ya fue evaluado." }),
            FeedbackActionStatus.FeedbackAlreadyExists => Conflict(new { message = "Este diagnóstico ya tiene feedback." }),
            FeedbackActionStatus.TechnicianNotFound => NotFound(new { message = "El técnico no existe." }),
            _ => StatusCode(500)
        };
    }

    [HttpGet("GetByDiagnosis/{diagnosisId:guid}")]
    public async Task<ActionResult<List<FeedbackResponse>>> GetByDiagnosis(Guid diagnosisId)
    {
        return Ok(await _feedbackService.GetByDiagnosisAsync(diagnosisId));
    }

    [HttpGet("GetRecentFeedbacks")]
    public async Task<ActionResult<List<FeedbackResponse>>> GetRecent([FromQuery] int take = 20)
    {
        return Ok(await _feedbackService.GetRecentAsync(take));
    }

    [HttpGet("GetMetrics")]
    public async Task<ActionResult<AIMetricsResponse>> GetMetrics()
    {
        return Ok(await _feedbackService.GetMetricsAsync());
    }
}
