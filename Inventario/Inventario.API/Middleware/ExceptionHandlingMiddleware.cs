using System.Text.Json;
using Inventario.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventario.API.Middleware;

/// <summary>Convierte las excepciones del negocio en respuestas JSON { "error": "mensaje" }.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente cerró la conexión: no hay a quién responder.
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, body) = Map(ex);
            if (status >= StatusCodes.Status500InternalServerError) logger.LogError(ex, "Error no controlado");

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(body);
        }
    }

    private static (int Status, object Body) Map(Exception ex) => ex switch
    {
        NotFoundException e => (StatusCodes.Status404NotFound, new { error = e.Message }),
        ConflictException e => (StatusCodes.Status409Conflict, new { error = e.Message, id = e.ExistingId }),
        BusinessRuleException e => (StatusCodes.Status400BadRequest, new { error = e.Message }),
        UnauthorizedException e => (StatusCodes.Status401Unauthorized, new { error = e.Message }),
        StorageUnavailableException e => (StatusCodes.Status503ServiceUnavailable, new { error = e.Message }),

        // Excepciones propias del módulo DailyTasks.
        DailyTasks.Core.Exceptions.NotFoundException e => (StatusCodes.Status404NotFound, new { error = e.Message }),
        DailyTasks.Core.Exceptions.ConflictException e => (StatusCodes.Status409Conflict, new { error = e.Message }),
        DailyTasks.Core.Exceptions.BusinessRuleException e => (StatusCodes.Status400BadRequest, new { error = e.Message }),
        DailyTasks.Core.Exceptions.UnauthorizedException e => (StatusCodes.Status401Unauthorized, new { error = e.Message }),
        DailyTasks.Core.Exceptions.ForbiddenException e => (StatusCodes.Status403Forbidden, new { error = e.Message }),

        UnauthorizedAccessException e => (StatusCodes.Status401Unauthorized, new { error = e.Message }),
        KeyNotFoundException e => (StatusCodes.Status404NotFound, new { error = e.Message }),
        InvalidOperationException e => (StatusCodes.Status400BadRequest, new { error = e.Message }),
        BadHttpRequestException => (StatusCodes.Status400BadRequest, new { error = "La solicitud no es válida." }),
        JsonException => (StatusCodes.Status400BadRequest, new { error = "El cuerpo de la solicitud no es válido." }),

        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict,
            new { error = "El registro cambió mientras se guardaba. Recarga e intenta de nuevo." }),

        DbUpdateException e when e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } =>
            (StatusCodes.Status409Conflict, new { error = "Ya existe un registro con esos datos." }),
        DbUpdateException e when e.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } =>
            (StatusCodes.Status400BadRequest, new { error = "La referencia indicada no existe o está en uso." }),
        DbUpdateException e when e.InnerException is PostgresException { SqlState: PostgresErrorCodes.NotNullViolation } =>
            (StatusCodes.Status400BadRequest, new { error = "Faltan datos obligatorios." }),
        DbUpdateException => (StatusCodes.Status500InternalServerError,
            new { error = "No se pudo guardar la información." }),

        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } =>
            (StatusCodes.Status409Conflict, new { error = "Ya existe un registro con esos datos." }),

        _ => (StatusCodes.Status500InternalServerError,
            new { error = "Ocurrió un error inesperado. Intenta de nuevo." })
    };
}
