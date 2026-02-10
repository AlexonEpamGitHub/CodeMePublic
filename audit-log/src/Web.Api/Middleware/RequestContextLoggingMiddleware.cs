using Microsoft.Extensions.Primitives;

namespace Web.Api.Middleware;

public class RequestContextLoggingMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeaderName = "X-Request-ID";

    public async Task Invoke(HttpContext context, ILogger<RequestContextLoggingMiddleware> logger)
    {
        string correlationId = context.Items[CorrelationIdHeaderName]?.ToString() ?? string.Empty;

        using (logger.BeginScope(new { CorrelationId = correlationId }))
        {
            await next(context);
        }
    }
}
