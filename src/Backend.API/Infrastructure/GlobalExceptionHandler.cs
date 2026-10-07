using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Infrastructure;

// Manejo global de errores: nunca devuelve detalles internos al cliente,
// registra el error con un identificador para poder rastrearlo en el log.
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;
        if (httpContext.Response.HasStarted)
        {
            _logger.LogError(exception, "Error después de iniciar la respuesta ({TraceId})", traceId);
            return false;
        }

        int statusCode;
        string message;

        switch (exception)
        {
            case DbUpdateConcurrencyException:
                statusCode = StatusCodes.Status409Conflict;
                message = "El registro fue modificado por otra operación. Vuelve a cargarlo e inténtalo de nuevo.";
                _logger.LogWarning(exception, "Conflicto de concurrencia ({TraceId})", traceId);
                break;
            case DbUpdateException dbException when DbErrors.IsUniqueViolation(dbException):
                statusCode = StatusCodes.Status409Conflict;
                message = "Ya existe un registro con esos datos o la operación se ejecutó dos veces.";
                _logger.LogWarning(exception, "Violación de unicidad ({TraceId})", traceId);
                break;
            case DbUpdateException dbException when DbErrors.IsForeignKeyViolation(dbException):
                statusCode = StatusCodes.Status409Conflict;
                message = "No se puede completar la operación porque el registro tiene información relacionada.";
                _logger.LogWarning(exception, "Violación de llave foránea ({TraceId})", traceId);
                break;
            case BadHttpRequestException badRequest:
                statusCode = badRequest.StatusCode;
                message = statusCode == StatusCodes.Status413PayloadTooLarge
                    ? "La petición es demasiado grande."
                    : "La petición no es válida.";
                _logger.LogWarning(exception, "Petición inválida ({TraceId})", traceId);
                break;
            case OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested:
                // El cliente cerró la conexión; no hay a quién responder.
                return true;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = "Ocurrió un error inesperado. Si persiste, informa este código al administrador.";
                _logger.LogError(exception, "Error no controlado ({TraceId})", traceId);
                break;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { message, traceId }, cancellationToken);
        return true;
    }
}
