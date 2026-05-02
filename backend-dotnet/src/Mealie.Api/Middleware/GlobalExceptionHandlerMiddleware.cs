using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace Mealie.Api.Middleware;

public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            object body = env.IsDevelopment()
                ? new { detail = ex.Message, exception = ex.GetType().FullName, stackTrace = ex.StackTrace }
                : new { detail = "Internal server error" };

            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }
}
