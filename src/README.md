# E470 Audit Log - Wolverine with SQL Server Transport

This solution demonstrates a modern .NET 9 microservices architecture using Wolverine message framework with SQL Server as the transactional inbox/outbox transport.

## Architecture Overview

The solution consists of:

1. **E470.AuditLog.Producer** - Publishes audit log events
2. **E470.AuditLog.Consumer** - Consumes and processes audit log events
3. **E470.AuditLog.Contracts** - Shared message contracts and domain models
4. **E470.AuditLog.Producer.AppHost** - .NET Aspire orchestration host
5. **E470.AuditLog.Producer.ServiceDefaults** - Shared service defaults (health checks, telemetry)

## Key Features

### Wolverine SQL Server Transport

Both Producer and Consumer projects are configured with:

- **SQL Server Persistence**: Messages are stored in SQL Server for durability
- **Transactional Inbox/Outbox Pattern**: Ensures exactly-once message delivery
- **Entity Framework Core Integration**: Database operations and message publishing in same transaction
- **Durable Local Queues**: Messages survive application restarts
- **Automatic Retry**: Failed messages are automatically retried with exponential backoff

### Technology Stack

- **.NET 9.0**
- **ASP.NET Core Web API**
- **Wolverine 5.16.2** - Message framework
- **Entity Framework Core** - ORM with SQL Server
- **.NET Aspire** - Orchestration and service defaults
- **SQL Server** - Message persistence and application database

## Project Structure

```
src/
├── E470.AuditLog.Contracts/          # Shared contracts
│   ├── Messages/
│   │   └── AuditLogCreated.cs        # Message contract
│   └── Models/
│       └── AuditLogEntry.cs          # Domain entity
│
├── E470.AuditLog.Produser/           # Producer service
│   ├── Controllers/
│   │   ├── AuditLogController.cs     # API for publishing events
│   │   └── WeatherForecastController.cs
│   ├── Data/
│   │   └── AuditLogDbContext.cs      # EF Core DbContext
│   └── Program.cs                     # Wolverine configuration
│
├── E470.AuditLog.Consumer/           # Consumer service
│   ├── Controllers/
│   │   ├── AuditLogQueryController.cs # Query API
│   │   └── WeatherForecastController.cs
│   ├── Data/
│   │   └── AuditLogDbContext.cs      # EF Core DbContext
│   ├── Handlers/
│   │   └── AuditLogCreatedHandler.cs # Message handler
│   └── Program.cs                     # Wolverine configuration
│
└── E470.AuditLog.Produser.AppHost/   # Aspire host
    └── AppHost.cs                     # Infrastructure orchestration
```

## Wolverine Configuration

### Producer Configuration

```csharp
builder.Host.UseWolverine(opts =>
{
    // SQL Server-backed message storage
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");

    // Entity Framework Core transaction support
    opts.UseEntityFrameworkCoreTransactions();

    // Durable inbox/outbox
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableOutbox();
});
```

### Consumer Configuration

```csharp
builder.Host.UseWolverine(opts =>
{
    // SQL Server-backed message storage
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");

    // Entity Framework Core transaction support
    opts.UseEntityFrameworkCoreTransactions();

    // Durable inbox/outbox
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableInbox();
    
    // Listen to local queues for messages
    opts.ListenToLocalQueue("auditlog");
});
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop (for SQL Server container)
- Visual Studio 2022 or Visual Studio Code with C# Dev Kit

### Running the Solution

1. **Clone the repository**

2. **Set SQL Server password as user secret**:
   ```bash
   cd src/E470.AuditLog.Produser.AppHost
   dotnet user-secrets set "Parameters:sql-password" "YourStrongPassword123!"
   ```

3. **Run with .NET Aspire**:
   ```bash
   cd src/E470.AuditLog.Produser.AppHost
   dotnet run
   ```

   This will:
   - Start SQL Server in a Docker container
   - Create the database "message-bus-mssql"
   - Start the Producer API
   - Start the Consumer API
   - Initialize Wolverine message storage schema

4. **Access the Aspire Dashboard**:
   - Open browser to https://localhost:17247 (or the URL shown in console)
   - View service health, logs, traces, and metrics

### Database Initialization

Wolverine automatically creates the necessary tables in SQL Server:

- `wolverine.wolverine_incoming_messages` - Inbox table
- `wolverine.wolverine_outgoing_messages` - Outbox table
- `wolverine.wolverine_dead_letters` - Failed messages
- Application tables (via EF Core migrations if configured)

### Testing the Flow

#### 1. Create an Audit Log Entry (Producer)

```bash
curl -X POST https://localhost:7001/api/auditlog \
  -H "Content-Type: application/json" \
  -d '{
    "action": "UserLogin",
    "userId": "user123",
    "details": "User logged in successfully",
    "resource": "/api/auth/login"
  }'
```

Response:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### 2. Query Processed Audit Logs (Consumer)

```bash
# Get all processed logs
curl https://localhost:7002/api/auditlogquery/processed

# Get specific log
curl https://localhost:7002/api/auditlogquery/3fa85f64-5717-4562-b3fc-2c963f66afa6

# Get logs by user
curl https://localhost:7002/api/auditlogquery/by-user/user123

# Get statistics
curl https://localhost:7002/api/auditlogquery/statistics
```

## Message Flow

1. **Client** sends POST request to **Producer API**
2. **Producer** saves `AuditLogEntry` to database
3. **Producer** publishes `AuditLogCreated` message via Wolverine
4. **Wolverine** stores message in SQL Server outbox (`wolverine_outgoing_messages`)
5. **Wolverine** delivers message to **Consumer** inbox (`wolverine_incoming_messages`)
6. **Consumer Handler** processes message within EF Core transaction
7. **Consumer** saves processed entry to its database
8. **Wolverine** marks message as completed and removes from inbox

## Transactional Guarantees

### Producer Side (Outbox Pattern)

When publishing a message:
1. Application data is saved to database
2. Message is written to outbox table
3. Both operations commit in single transaction
4. Background worker sends messages from outbox
5. Message removed from outbox after successful delivery

**Benefit**: If application crashes after database save but before message send, message will still be delivered when app restarts.

### Consumer Side (Inbox Pattern)

When processing a message:
1. Message written to inbox table
2. Handler executes within transaction
3. Application data saved to database
4. Message marked as processed in inbox
5. All operations commit in single transaction

**Benefit**: If handler crashes mid-processing, message remains in inbox and will be retried. Duplicate messages are handled via idempotency check.

## Configuration

### Connection Strings

Connection strings are injected by .NET Aspire:

```json
{
  "ConnectionStrings": {
    "message-bus-mssql": "Server=localhost,5051;Database=message-bus-mssql;User Id=sa;Password=***;TrustServerCertificate=True"
  }
}
```

### Wolverine Options

Key configuration options in `Program.cs`:

```csharp
// Message persistence
opts.PersistMessagesWithSqlServer(connectionString, schemaName);

// Transaction integration
opts.UseEntityFrameworkCoreTransactions();

// Durability policies
opts.Policies.UseDurableLocalQueues();
opts.Policies.UseDurableInbox();
opts.Policies.UseDurableOutbox();

// Queue configuration
opts.ListenToLocalQueue("queue-name");
opts.PublishMessage<MessageType>().ToLocalQueue("queue-name");
```

## Monitoring and Observability

### Aspire Dashboard

The .NET Aspire dashboard provides:
- **Service Health**: Real-time health status
- **Logs**: Centralized log viewing
- **Traces**: Distributed tracing with OpenTelemetry
- **Metrics**: Performance metrics and resource utilization

### Wolverine Diagnostics

Query message status directly from SQL Server:

```sql
-- Check outbox messages
SELECT * FROM wolverine.wolverine_outgoing_messages;

-- Check inbox messages
SELECT * FROM wolverine.wolverine_incoming_messages;

-- Check dead letters (failed messages)
SELECT * FROM wolverine.wolverine_dead_letters;
```

## Error Handling

### Automatic Retry

Wolverine automatically retries failed messages with exponential backoff:
- Attempt 1: Immediate
- Attempt 2: 1 second delay
- Attempt 3: 5 seconds delay
- Attempt 4: 30 seconds delay
- Attempt 5: Moved to dead letter queue

### Dead Letter Queue

Failed messages after max retries are moved to `wolverine_dead_letters` table for manual investigation.

### Idempotency

Handlers implement idempotency checks to safely handle duplicate messages:

```csharp
var existingEntry = await dbContext.AuditLogEntries
    .FirstOrDefaultAsync(e => e.Id == message.Id, cancellationToken);

if (existingEntry != null)
{
    _logger.LogWarning("Already processed, skipping");
    return;
}
```

## Best Practices

1. **Use [Transactional] Attribute**: Wrap handlers in database transactions
2. **Implement Idempotency**: Check for duplicate processing
3. **Use Records for Messages**: Immutable message contracts
4. **Schema Versioning**: Version message contracts carefully
5. **Monitor Dead Letters**: Set up alerts for failed messages
6. **Test Failure Scenarios**: Simulate crashes to verify recovery

## Troubleshooting

### Messages Not Being Delivered

1. Check SQL Server is running: `docker ps`
2. Verify connection string in Aspire dashboard
3. Check Wolverine tables exist:
   ```sql
   SELECT * FROM INFORMATION_SCHEMA.TABLES 
   WHERE TABLE_SCHEMA = 'wolverine';
   ```

### Database Errors

1. Ensure SQL Server container is healthy
2. Check credentials are correct
3. Verify database "message-bus-mssql" exists
4. Run EF Core migrations if needed:
   ```bash
   dotnet ef database update
   ```

### Performance Issues

1. Monitor message throughput in dashboard
2. Check for long-running handlers
3. Consider adding indexes on frequently queried columns
4. Scale out consumers if needed

## Further Reading

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Inbox Pattern](https://microservices.io/patterns/communication-style/idempotent-consumer.html)

## License

This is a sample/demo project for educational purposes.
