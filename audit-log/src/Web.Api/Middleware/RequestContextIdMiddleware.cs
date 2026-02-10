namespace Web.Api.Middleware;

public class RequestContextIdMiddleware
{
    private const string _correlationIdHeader = "X-Request-ID";
    private readonly RequestDelegate _next;

    public RequestContextIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = context.Request.Headers[_correlationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        context.Items[_correlationIdHeader] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_correlationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
