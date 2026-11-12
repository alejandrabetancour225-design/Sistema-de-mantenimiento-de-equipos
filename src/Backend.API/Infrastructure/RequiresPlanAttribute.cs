using Backend.API.Data;
using Backend.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure;

// Restringe un controlador o acción a las organizaciones con cierto plan (Q25).
// Responde 403 con un mensaje claro si la organización no tiene el plan.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresPlanAttribute : Attribute, IAsyncActionFilter
{
    private readonly MembershipPlan _plan;

    public RequiresPlanAttribute(MembershipPlan plan)
    {
        _plan = plan;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tenant = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var orgId = tenant.OrganizationId;

        var plan = orgId is { } id && id != Guid.Empty
            ? await db.Organizations.AsNoTracking().Where(o => o.Id == id).Select(o => (MembershipPlan?)o.Plan).FirstOrDefaultAsync()
            : null;

        if (plan is null || plan < _plan)
        {
            context.Result = new ObjectResult(new
            {
                message = $"Esta función requiere el plan {PlanNames.Display(_plan)}. Pide al administrador de la plataforma que lo active."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }
}

public static class PlanNames
{
    public static string Display(MembershipPlan plan) => plan switch
    {
        MembershipPlan.BASICO => "Básico",
        MembershipPlan.INTEGRACIONES => "Integraciones",
        _ => plan.ToString()
    };
}
