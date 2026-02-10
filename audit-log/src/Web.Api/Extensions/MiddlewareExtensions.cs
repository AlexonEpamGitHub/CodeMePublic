using Web.Api.Middleware;

namespace Web.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextIdMiddleware>();
        app.UseMiddleware<RequestContextLoggingMiddleware>();
        return app;
    }
}
