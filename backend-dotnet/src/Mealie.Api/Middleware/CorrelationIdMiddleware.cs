using Serilog.Context;

namespace Mealie.Api.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        using (LogContext.PushProperty("RequestId", correlationId))
        {
            await next(context);
        }
    }
}
