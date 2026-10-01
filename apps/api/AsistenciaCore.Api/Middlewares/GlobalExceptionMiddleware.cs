using System.Net;
using System.Text.Json;

namespace AsistenciaCore.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió una excepción no controlada en la API: {Message}", ex.Message);
            await ManejarExcepcionAsync(context, ex);
        }
    }

    private static Task ManejarExcepcionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            ArgumentException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;

        var respuestaError = new
        {
            Status = context.Response.StatusCode,
            Titulo = "Error interno en la API de Asistencia",
            Detalle = exception.Message,
            Path = context.Request.Path.Value,
            Timestamp = DateTime.UtcNow
        };

        var opcionesJson = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsync(JsonSerializer.Serialize(respuestaError, opcionesJson));
    }
}