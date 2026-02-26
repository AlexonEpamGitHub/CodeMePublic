# Wolverine Configuration Examples

This document provides additional configuration examples and advanced scenarios for Wolverine with SQL Server transport.

## Table of Contents

1. [Basic Configuration](#basic-configuration)
2. [Advanced Retry Policies](#advanced-retry-policies)
3. [Message Routing](#message-routing)
4. [Error Handling](#error-handling)
5. [Performance Tuning](#performance-tuning)
6. [Multi-Tenant Scenarios](#multi-tenant-scenarios)

## Basic Configuration

### Minimal Producer Setup

```csharp
builder.Host.UseWolverine(opts =>
{
    // SQL Server persistence
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");
    
    // EF Core integration
    opts.UseEntityFrameworkCoreTransactions();
    
    // Enable outbox pattern
    opts.Policies.UseDurableOutbox();
});
```

### Minimal Consumer Setup

```csharp
builder.Host.UseWolverine(opts =>
{
    // SQL Server persistence
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");
    
    // EF Core integration
    opts.UseEntityFrameworkCoreTransactions();
    
    // Enable inbox pattern
    opts.Policies.UseDurableInbox();
    
    // Auto-discover handlers
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
});
```

## Advanced Retry Policies

### Custom Retry Schedule

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");
    
    // Custom retry schedule
    opts.Policies.OnException<SqlException>()
        .RetryWithCooldown(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1)
        );
});
```

### Message-Specific Retry Policy

```csharp
// In message handler
public class AuditLogCreatedHandler
{
    [RetryNow(typeof(SqlException), maxAttempts: 3)]
    [RetryWithCooldown(typeof(HttpRequestException), 
        cooldownInMilliseconds: new[] { 1000, 5000, 15000 })]
    public async Task Handle(AuditLogCreated message, AuditLogDbContext dbContext)
    {
        // Handler logic
    }
}
```

### Exponential Backoff

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.Policies.OnException<TimeoutException>()
        .RetryWithExponentialBackoff(
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromMinutes(5),
            maxAttempts: 5
        );
});
```

## Message Routing

### Local Queue Routing

```csharp
builder.Host.UseWolverine(opts =>
{
    // Publish to named queue
    opts.PublishMessage<AuditLogCreated>()
        .ToLocalQueue("audit-logs");
    
    opts.PublishMessage<UserActivityLogged>()
        .ToLocalQueue("user-activities");
    
    // Listen to multiple queues
    opts.ListenToLocalQueue("audit-logs")
        .MaximumParallelMessages(10);
    
    opts.ListenToLocalQueue("user-activities")
        .MaximumParallelMessages(5);
});
```

### Conditional Routing

```csharp
builder.Host.UseWolverine(opts =>
{
    // Route based on message properties
    opts.PublishMessage<AuditLogCreated>()
        .ToLocalQueue("critical-logs")
        .When(msg => msg.Action.Contains("Security"));
    
    opts.PublishMessage<AuditLogCreated>()
        .ToLocalQueue("standard-logs")
        .When(msg => !msg.Action.Contains("Security"));
});
```

### Delayed Message Processing

```csharp
// In controller or service
await _messageBus.ScheduleAsync(
    new AuditLogCreated { /* ... */ },
    TimeSpan.FromMinutes(5)
);

// Or with specific time
await _messageBus.ScheduleAsync(
    new AuditLogCreated { /* ... */ },
    DateTimeOffset.Now.AddHours(1)
);
```

## Error Handling

### Move to Dead Letter Queue

```csharp
builder.Host.UseWolverine(opts =>
{
    // Move to dead letter after 5 attempts
    opts.Policies.OnException<InvalidOperationException>()
        .MoveToDeadLetterQueueAfterAttempts(5);
});
```

### Custom Error Handler

```csharp
public class CustomErrorHandler : IFailureAction
{
    private readonly ILogger<CustomErrorHandler> _logger;
    
    public CustomErrorHandler(ILogger<CustomErrorHandler> logger)
    {
        _logger = logger;
    }
    
    public async Task ProcessAsync(Envelope envelope, Exception exception)
    {
        _logger.LogError(exception, 
            "Failed to process message {MessageType} with ID {MessageId}", 
            envelope.MessageType, 
            envelope.Id);
        
        // Custom logic: send alert, log to external system, etc.
        await SendAlertAsync(envelope, exception);
    }
}

// Register in Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.Policies.OnException<Exception>()
        .MoveToDeadLetterQueueAfterAttempts(3)
        .Then.ExecuteFailureAction<CustomErrorHandler>();
});
```

### Discard Invalid Messages

```csharp
builder.Host.UseWolverine(opts =>
{
    // Discard after validation failures
    opts.Policies.OnException<ValidationException>()
        .Discard();
    
    // Log and discard
    opts.Policies.OnException<ArgumentException>()
        .Discard()
        .WithAction((envelope, ex) => 
        {
            Console.WriteLine($"Discarded invalid message: {ex.Message}");
        });
});
```

## Performance Tuning

### Parallel Processing

```csharp
builder.Host.UseWolverine(opts =>
{
    // Process up to 20 messages concurrently
    opts.ListenToLocalQueue("audit-logs")
        .MaximumParallelMessages(20);
    
    // Single-threaded processing for order-sensitive messages
    opts.ListenToLocalQueue("sequential-logs")
        .MaximumParallelMessages(1);
});
```

### Batch Processing

```csharp
// Handler that processes messages in batches
public class BatchAuditLogHandler
{
    [Transactional]
    public async Task Handle(
        AuditLogCreated[] messages, 
        AuditLogDbContext dbContext)
    {
        var entries = messages.Select(m => new AuditLogEntry
        {
            Id = m.Id,
            Action = m.Action,
            // ... map properties
        }).ToList();
        
        dbContext.AuditLogEntries.AddRange(entries);
        await dbContext.SaveChangesAsync();
    }
}

// Configure batch size
builder.Host.UseWolverine(opts =>
{
    opts.ListenToLocalQueue("audit-logs")
        .ProcessInline() // Process immediately
        .BatchMessagesOf<AuditLogCreated>(batchSize: 50);
});
```

### Message Timeout

```csharp
builder.Host.UseWolverine(opts =>
{
    // Set execution timeout
    opts.Policies.ConfigureConventionalLocalRouting()
        .ExecutionTimeout(TimeSpan.FromSeconds(30));
});

// Or per handler
public class AuditLogCreatedHandler
{
    [ExecutionTimeout(seconds: 60)]
    public async Task Handle(AuditLogCreated message)
    {
        // Handler logic
    }
}
```

### Memory Optimization

```csharp
builder.Host.UseWolverine(opts =>
{
    // Limit durability agent worker count
    opts.Durability.DurabilityAgentWorkerCount = 2;
    
    // Adjust polling interval
    opts.Durability.ScheduledJobPollingTime = TimeSpan.FromSeconds(5);
    
    // Configure message recovery
    opts.Durability.RecoveryBatchSize = 500;
});
```

## Multi-Tenant Scenarios

### Tenant Isolation

```csharp
// Add tenant ID to messages
public record AuditLogCreated
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    // ... other properties
}

// Configure tenant-aware routing
builder.Host.UseWolverine(opts =>
{
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");
    
    // Route by tenant
    opts.PublishMessage<AuditLogCreated>()
        .ToLocalQueue(msg => $"tenant-{msg.TenantId}-logs");
});
```

### Tenant-Specific Connection Strings

```csharp
public class TenantConnectionProvider
{
    private readonly IConfiguration _configuration;
    
    public string GetConnectionString(string tenantId)
    {
        return _configuration.GetConnectionString($"Tenant-{tenantId}");
    }
}

// In handler
public class AuditLogCreatedHandler
{
    private readonly TenantConnectionProvider _connectionProvider;
    private readonly IDbContextFactory<AuditLogDbContext> _contextFactory;
    
    [Transactional]
    public async Task Handle(AuditLogCreated message)
    {
        var connectionString = _connectionProvider
            .GetConnectionString(message.TenantId);
        
        var optionsBuilder = new DbContextOptionsBuilder<AuditLogDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        
        await using var dbContext = _contextFactory.CreateDbContext();
        
        // Process message with tenant-specific context
    }
}
```

## Message Versioning

### Version 1 Message

```csharp
public record AuditLogCreatedV1
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}
```

### Version 2 Message (Extended)

```csharp
public record AuditLogCreatedV2
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty; // New field
    public Dictionary<string, string> Metadata { get; init; } = new(); // New field
}
```

### Handling Both Versions

```csharp
// Handler for V1
public class AuditLogCreatedV1Handler
{
    public async Task Handle(AuditLogCreatedV1 message, IMessageBus bus)
    {
        // Convert V1 to V2
        var v2Message = new AuditLogCreatedV2
        {
            Id = message.Id,
            Action = message.Action,
            UserId = message.UserId,
            IpAddress = "Unknown", // Default for missing field
            Metadata = new Dictionary<string, string>()
        };
        
        // Forward to V2 handler
        await bus.PublishAsync(v2Message);
    }
}

// Handler for V2
public class AuditLogCreatedV2Handler
{
    [Transactional]
    public async Task Handle(AuditLogCreatedV2 message, AuditLogDbContext dbContext)
    {
        // Process the message
    }
}
```

## Monitoring and Diagnostics

### Custom Logging

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.Policies.OnSuccess((envelope, context) =>
    {
        var logger = context.GetRequiredService<ILogger<Program>>();
        logger.LogInformation(
            "Successfully processed message {MessageType} with ID {MessageId} in {Duration}ms",
            envelope.MessageType,
            envelope.Id,
            envelope.ExecutionTime?.TotalMilliseconds
        );
    });
    
    opts.Policies.OnException<Exception>((envelope, exception, context) =>
    {
        var logger = context.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception,
            "Failed to process message {MessageType} with ID {MessageId}, Attempt {Attempt}",
            envelope.MessageType,
            envelope.Id,
            envelope.Attempts
        );
    });
});
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<WolverineHealthCheck>("wolverine")
    .AddSqlServer(connectionString, name: "wolverine-storage");

public class WolverineHealthCheck : IHealthCheck
{
    private readonly IDbConnection _connection;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken)
    {
        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) 
                FROM wolverine.wolverine_dead_letters 
                WHERE received_at > DATEADD(hour, -1, GETUTCDATE())";
            
            var deadLetterCount = (int)await cmd.ExecuteScalarAsync(cancellationToken);
            
            if (deadLetterCount > 100)
            {
                return HealthCheckResult.Degraded(
                    $"High number of dead letters: {deadLetterCount}");
            }
            
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Failed to check Wolverine health", 
                ex);
        }
    }
}
```

### Metrics

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Wolverine");
        
        // Custom metrics
        metrics.AddView("wolverine.message.processed",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new[] { 10, 50, 100, 500, 1000 }
            });
    });
```

## Testing

### Integration Test Setup

```csharp
public class WolverineIntegrationTestFixture : IAsyncLifetime
{
    private IHost _host = null!;
    
    public async Task InitializeAsync()
    {
        _host = Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.PersistMessagesWithSqlServer(connectionString, "wolverine");
                opts.UseEntityFrameworkCoreTransactions();
                opts.Policies.UseDurableLocalQueues();
            })
            .Build();
        
        await _host.StartAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}

[Collection("Wolverine")]
public class AuditLogTests : IClassFixture<WolverineIntegrationTestFixture>
{
    [Fact]
    public async Task Should_Process_Message_Successfully()
    {
        // Test implementation
    }
}
```

## Best Practices Summary

1. ✅ **Always use transactions** - Wrap handlers with `[Transactional]`
2. ✅ **Implement idempotency** - Check for duplicates in handlers
3. ✅ **Use immutable messages** - Define messages as records
4. ✅ **Handle failures gracefully** - Configure appropriate retry policies
5. ✅ **Monitor dead letters** - Set up alerts for failed messages
6. ✅ **Version your messages** - Plan for schema evolution
7. ✅ **Test failure scenarios** - Verify retry and recovery behavior
8. ✅ **Keep handlers focused** - Single responsibility per handler
9. ✅ **Use dependency injection** - Inject services into handlers
10. ✅ **Log appropriately** - Balance detail with noise

## Additional Resources

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [Sample Code Repository](https://github.com/JasperFx/wolverine)
- [Transactional Messaging Patterns](https://microservices.io/patterns/data/transactional-outbox.html)
