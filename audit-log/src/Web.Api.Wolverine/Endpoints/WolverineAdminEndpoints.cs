using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Http;
using Web.Api.Wolverine.Infrastructure.Persistence;

namespace Web.Api.Wolverine.Endpoints;

/// <summary>
/// Administrative endpoints for monitoring Wolverine SQL Server persistence
/// </summary>
public static class WolverineAdminEndpoints
{
    public static IEndpointRouteBuilder MapWolverineAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/wolverine")
            .WithTags("Wolverine Admin")
            .WithOpenApi();

        group.MapGet("/stats", GetMessageStatistics)
            .WithName("GetWolverineStats")
            .WithSummary("Get current message statistics")
            .Produces<MessageStatistics>();

        group.MapGet("/schema", GetDatabaseSchema)
            .WithName("GetWolverineSchema")
            .WithSummary("Get SQL schema creation script")
            .Produces<string>();

        group.MapPost("/clear", ClearAllMessages)
            .WithName("ClearWolverineMessages")
            .WithSummary("Clear all messages (use with caution)")
            .Produces<ClearResult>();

        group.MapGet("/health", GetWolverineHealth)
            .WithName("GetWolverineHealth")
            .WithSummary("Check Wolverine system health")
            .Produces<HealthStatus>();

        group.MapPost("/release-stuck", ReleaseStuckMessages)
            .WithName("ReleaseStuckMessages")
            .WithSummary("Release messages that are stuck processing")
            .Produces<ReleaseResult>();

        return app;
    }

    /// <summary>
    /// Get current message statistics from SQL Server
    /// </summary>
    [WolverineGet("/stats")]
    public static async Task<MessageStatistics> GetMessageStatistics(IHost host)
    {
        return await WolverineSqlServerSetup.GetMessageStatisticsAsync(host);
    }

    /// <summary>
    /// Get the SQL schema script for manual database setup
    /// </summary>
    [WolverineGet("/schema")]
    public static async Task<IResult> GetDatabaseSchema(IHost host)
    {
        try
        {
            var script = await WolverineSqlServerSetup.GenerateSchemaScriptAsync(host);
            return Results.Text(script, "text/plain");
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to generate schema",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Clear all messages from the system (development only)
    /// </summary>
    [WolverinePost("/clear")]
    public static async Task<ClearResult> ClearAllMessages(
        IHost host,
        ILogger<Program> logger,
        [FromQuery] bool confirm = false)
    {
        if (!confirm)
        {
            return new ClearResult
            {
                Success = false,
                Message = "Must provide ?confirm=true to clear all messages"
            };
        }

        try
        {
            await WolverineSqlServerSetup.ClearAllMessagesAsync(host, logger);
            return new ClearResult
            {
                Success = true,
                Message = "All messages cleared successfully"
            };
        }
        catch (Exception ex)
        {
            return new ClearResult
            {
                Success = false,
                Message = $"Failed to clear messages: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get comprehensive health status of Wolverine system
    /// </summary>
    [WolverineGet("/health")]
    public static async Task<HealthStatus> GetWolverineHealth(
        IHost host,
        IConfiguration config)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var status = new HealthStatus { Timestamp = DateTime.UtcNow };

        try
        {
            // Check database connection
            var connectionString = config.GetConnectionString("WolverineDatabase");
            await WolverineSqlServerSetup.ValidateSqlServerConnectionAsync(connectionString!, logger);
            status.DatabaseConnected = true;

            // Get message stats
            var stats = await WolverineSqlServerSetup.GetMessageStatisticsAsync(host);
            status.Statistics = stats;

            // Determine overall health
            if (stats.TotalCount > 10000)
            {
                status.Status = "Degraded";
                status.Message = $"High message count: {stats.TotalCount} messages pending";
            }
            else if (stats.TotalCount > 1000)
            {
                status.Status = "Warning";
                status.Message = $"Elevated message count: {stats.TotalCount} messages pending";
            }
            else
            {
                status.Status = "Healthy";
                status.Message = "System is operating normally";
            }
        }
        catch (Exception ex)
        {
            status.Status = "Unhealthy";
            status.Message = ex.Message;
            status.DatabaseConnected = false;
        }

        return status;
    }

    /// <summary>
    /// Release messages that are stuck in processing state
    /// </summary>
    [WolverinePost("/release-stuck")]
    public static async Task<ReleaseResult> ReleaseStuckMessages(
        IWolverineRuntime runtime,
        ILogger<Program> logger)
    {
        try
        {
            // This will release messages that have been claimed by nodes that are no longer active
            await runtime.Storage.Database.Admin.ReleaseAllOwnershipAsync();

            logger.LogInformation("Released all stuck messages");

            return new ReleaseResult
            {
                Success = true,
                Message = "Stuck messages have been released and will be reprocessed"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to release stuck messages");
            return new ReleaseResult
            {
                Success = false,
                Message = $"Failed to release messages: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Result of clearing messages
/// </summary>
public record ClearResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Result of releasing stuck messages
/// </summary>
public record ReleaseResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int MessagesReleased { get; init; }
}

/// <summary>
/// Overall health status of the Wolverine system
/// </summary>
public record HealthStatus
{
    public DateTime Timestamp { get; init; }
    public string Status { get; init; } = "Unknown";
    public string Message { get; init; } = string.Empty;
    public bool DatabaseConnected { get; init; }
    public MessageStatistics? Statistics { get; init; }
}

/// <summary>
/// Example endpoint showing transactional outbox pattern
/// </summary>
public static class TransactionalExampleEndpoints
{
    public static IEndpointRouteBuilder MapTransactionalExamples(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/examples/transactional")
            .WithTags("Transactional Examples")
            .WithOpenApi();

        group.MapPost("/create-order", CreateOrderWithNotifications)
            .WithName("CreateOrderTransactional")
            .WithSummary("Creates order with guaranteed notification delivery")
            .Produces<OrderCreatedResponse>();

        group.MapPost("/schedule-reminder", ScheduleReminder)
            .WithName("ScheduleReminder")
            .WithSummary("Schedule a reminder for future delivery")
            .Produces<ReminderScheduledResponse>();

        return app;
    }

    /// <summary>
    /// Example: Create order with transactional outbox
    /// The notification will be sent even if the app crashes after saving
    /// </summary>
    [WolverinePost("/create-order")]
    public static async Task<OrderCreatedResponse> CreateOrderWithNotifications(
        CreateOrderRequest request,
        IMessageBus bus,
        ILogger<Program> logger)
    {
        var orderId = Guid.NewGuid();

        logger.LogInformation("Creating order {OrderId} for customer {CustomerId}", 
            orderId, request.CustomerId);

        // Simulate saving to database
        // In real app, this would be wrapped in a transaction with EF Core

        // Publish notifications - these are stored in the outbox table
        // They will be sent AFTER the transaction commits
        await bus.PublishAsync(new OrderCreatedEvent(orderId, request.CustomerId, request.TotalAmount));
        await bus.PublishAsync(new SendEmailCommand(
            request.CustomerEmail, 
            "Order Confirmed", 
            $"Your order {orderId} has been confirmed"));

        logger.LogInformation("Order {OrderId} created with outbox notifications", orderId);

        return new OrderCreatedResponse
        {
            OrderId = orderId,
            Message = "Order created successfully. Notifications are being processed.",
            EstimatedDeliveryMinutes = 5
        };
    }

    /// <summary>
    /// Example: Schedule a message for future delivery
    /// </summary>
    [WolverinePost("/schedule-reminder")]
    public static async Task<ReminderScheduledResponse> ScheduleReminder(
        ScheduleReminderRequest request,
        IMessageBus bus,
        ILogger<Program> logger)
    {
        var scheduledTime = DateTime.UtcNow.AddMinutes(request.DelayMinutes);

        // Schedule message - it will be stored in SQL Server and delivered at the specified time
        await bus.ScheduleAsync(
            new SendReminderCommand(request.UserId, request.Message),
            scheduledTime);

        logger.LogInformation("Reminder scheduled for user {UserId} at {ScheduledTime}", 
            request.UserId, scheduledTime);

        return new ReminderScheduledResponse
        {
            ScheduledAt = scheduledTime,
            Message = "Reminder scheduled successfully"
        };
    }
}

// Request/Response DTOs
public record CreateOrderRequest(Guid CustomerId, string CustomerEmail, decimal TotalAmount);
public record OrderCreatedResponse
{
    public Guid OrderId { get; init; }
    public string Message { get; init; } = string.Empty;
    public int EstimatedDeliveryMinutes { get; init; }
}

public record ScheduleReminderRequest(Guid UserId, string Message, int DelayMinutes);
public record ReminderScheduledResponse
{
    public DateTime ScheduledAt { get; init; }
    public string Message { get; init; } = string.Empty;
}

// Events/Commands
public record OrderCreatedEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount);
public record SendEmailCommand(string To, string Subject, string Body);
public record SendReminderCommand(Guid UserId, string Message);
