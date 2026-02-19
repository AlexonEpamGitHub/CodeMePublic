using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wolverine;
using Web.Api.Wolverine.Infrastructure.Persistence;

namespace Web.Api.Wolverine.Infrastructure.HealthChecks;

/// <summary>
/// Health checks for Wolverine SQL Server persistence
/// </summary>
public static class WolverineHealthChecks
{
    /// <summary>
    /// Adds Wolverine-specific health checks
    /// </summary>
    public static IHealthChecksBuilder AddWolverineHealthChecks(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        // SQL Server connectivity check
        builder.AddSqlServer(
            connectionString: configuration.GetConnectionString("WolverineDatabase")!,
            name: "wolverine-sqlserver",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "db", "sql", "wolverine" });

        // Custom Wolverine storage check
        builder.AddCheck<WolverineStorageHealthCheck>(
            name: "wolverine-storage",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "wolverine", "storage" });

        // Message processing check
        builder.AddCheck<WolverineMessageProcessingHealthCheck>(
            name: "wolverine-messaging",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "wolverine", "messaging" });

        return builder;
    }

    /// <summary>
    /// Maps detailed health check endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapWolverineHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Overall health
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        });

        // Readiness probe (includes database)
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("wolverine"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        });

        // Liveness probe (basic checks only)
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false // Only checks if the app is running
        });

        // Wolverine-specific health
        endpoints.MapHealthChecks("/health/wolverine", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("wolverine"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        });

        return endpoints;
    }
}

/// <summary>
/// Health check for Wolverine storage accessibility
/// </summary>
public class WolverineStorageHealthCheck : IHealthCheck
{
    private readonly IWolverineRuntime _runtime;
    private readonly ILogger<WolverineStorageHealthCheck> _logger;

    public WolverineStorageHealthCheck(IWolverineRuntime runtime, ILogger<WolverineStorageHealthCheck> logger)
    {
        _runtime = runtime;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to access storage
            var storage = _runtime.Storage;
            
            // Check if database is accessible
            var isHealthy = await storage.Database.CheckConnectivityAsync(cancellationToken);

            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Wolverine storage is accessible");
            }

            return HealthCheckResult.Degraded("Wolverine storage connectivity issues");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wolverine storage health check failed");
            return HealthCheckResult.Unhealthy("Wolverine storage is not accessible", ex);
        }
    }
}

/// <summary>
/// Health check for Wolverine message processing
/// </summary>
public class WolverineMessageProcessingHealthCheck : IHealthCheck
{
    private readonly IWolverineRuntime _runtime;
    private readonly ILogger<WolverineMessageProcessingHealthCheck> _logger;

    public WolverineMessageProcessingHealthCheck(
        IWolverineRuntime runtime,
        ILogger<WolverineMessageProcessingHealthCheck> logger)
    {
        _runtime = runtime;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get message statistics
            var admin = _runtime.Storage.Database.Admin;
            var incomingCount = await admin.GetIncomingEnvelopeCountAsync();
            var outgoingCount = await admin.GetOutgoingEnvelopeCountAsync();
            var scheduledCount = await admin.GetScheduledEnvelopeCountAsync();

            var data = new Dictionary<string, object>
            {
                { "incoming_messages", incomingCount },
                { "outgoing_messages", outgoingCount },
                { "scheduled_messages", scheduledCount },
                { "total_messages", incomingCount + outgoingCount + scheduledCount }
            };

            // Consider unhealthy if there are too many pending messages
            var totalMessages = incomingCount + outgoingCount + scheduledCount;
            if (totalMessages > 10000)
            {
                return HealthCheckResult.Degraded(
                    $"High message count: {totalMessages} pending messages",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Message processing healthy. {totalMessages} messages in queues",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wolverine message processing health check failed");
            return HealthCheckResult.Unhealthy("Unable to check message processing status", ex);
        }
    }
}

/// <summary>
/// Custom health check response writer with detailed information
/// </summary>
public static class HealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            })
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
