using System.Net;
using System.Text.Json;

namespace CVMatchAI.API.Middleware;

/// <summary>
/// Middleware global para capturar excepciones no controladas y
/// devolverlas como JSON con el formato estándar de error.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Error no controlado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Determinar código de estado según el tipo de excepción
        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException         => HttpStatusCode.NotFound,
            ArgumentException           => HttpStatusCode.BadRequest,
            InvalidOperationException   => HttpStatusCode.BadRequest,
            _                           => HttpStatusCode.InternalServerError
        };

        var response = new
        {
            message = exception.GetBaseException().Message,
            statusCode = (int)statusCode
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        );
    }
}
