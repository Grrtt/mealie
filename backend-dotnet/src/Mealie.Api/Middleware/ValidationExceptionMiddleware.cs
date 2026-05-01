using System.Text.Json;
using FluentValidation;

namespace Mealie.Api.Middleware;

public class ValidationExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 422;
            context.Response.ContentType = "application/json";
            var errors = ex.Errors.Select(e => new
            {
                loc = new[] { "body", e.PropertyName },
                msg = e.ErrorMessage,
                type = e.ErrorCode
            });
            var body = JsonSerializer.Serialize(new { detail = errors });
            await context.Response.WriteAsync(body);
        }
    }
}
