using System.Net;
using System.Text.Json;

namespace PaymentService.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var message = env.IsDevelopment()
                ? $"{ex.GetType().Name}: {ex.Message} | {ex.InnerException?.Message}"
                : "An unexpected error occurred.";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
        }
    }
}

