# Wolverine SQL Server Transactional Inbox/Outbox

This document explains how Wolverine uses SQL Server for reliable message processing with transactional inbox and outbox patterns.

## 📚 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Setup](#setup)
- [Database Schema](#database-schema)
- [Configuration](#configuration)
- [Usage Patterns](#usage-patterns)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)

## 🎯 Overview

### What is Transactional Inbox/Outbox?

**Transactional Outbox**: Ensures messages are sent reliably by storing them in the database as part of the same transaction as your business data. Messages are then sent asynchronously by a background process.

**Transactional Inbox**: Ensures messages are processed exactly once by tracking message IDs in the database and deduplicating incoming messages.

### Benefits

✅ **Guaranteed Delivery** - Messages will be delivered even if the app crashes  
✅ **Exactly-Once Processing** - Automatic deduplication prevents duplicate processing  
✅ **Transaction Safety** - Messages and data are saved atomically  
✅ **Automatic Retries** - Failed messages are automatically retried with configurable policies  
✅ **Scheduled Messages** - Supports delayed/scheduled message delivery  
✅ **No External Dependencies** - Uses your existing SQL Server database

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Your Application                         │
│  ┌──────────────┐         ┌──────────────┐                 │
│  │   API Layer  │────────▶│   Handler    │                 │
│  └──────────────┘         └──────┬───────┘                 │
│                                   │                          │
│                          ┌────────▼────────┐                │
│                          │  Message Bus    │                │
│                          └────────┬────────┘                │
│                                   │                          │
└───────────────────────────────────┼──────────────────────────┘
                                    │
                    ┌───────────────▼──────────────┐
                    │      SQL Server              │
                    │  ┌────────────────────────┐  │
                    │  │ wolverine_outbox       │  │
                    │  │  - id                  │  │
                    │  │  - message_type        │  │
                    │  │  - body                │  │
                    │  │  - owner_id            │  │
                    │  └────────────────────────┘  │
                    │  ┌────────────────────────┐  │
                    │  │ wolverine_inbox        │  │
                    │  │  - id                  │  │
                    │  │  - status              │  │
                    │  │  - kept_until          │  │
                    │  └────────────────────────┘  │
                    │  ┌────────────────────────┐  │
                    │  │ wolverine_scheduled    │  │
                    │  │  - id                  │  │
                    │  │  - execution_time      │  │
                    │  └────────────────────────┘  │
                    └─────────────────────────────┘
                                    │
                    ┌───────────────▼──────────────┐
                    │   Durability Agent           │
                    │  (Background Process)        │
                    │  - Polls outbox every 5s     │
                    │  - Sends pending messages    │
                    │  - Executes scheduled jobs   │
                    │  - Retries failed messages   │
                    └──────────────────────────────┘
```

## 🚀 Setup

### 1. Prerequisites

```bash
# Ensure SQL Server is running
docker-compose up -d sqlserver
```

### 2. Install Required Packages

Already included in `Web.Api.Wolverine.csproj`:
```xml
<PackageReference Include="WolverineFx.SqlServer" />
<PackageReference Include="Microsoft.Data.SqlClient" />
```

### 3. Configuration

In `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "WolverineDatabase": "Server=localhost,1433;Database=WolverineDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  },
  "Wolverine": {
    "Durability": {
      "AgentEnabled": true,
      "ScheduledJobPollingTime": "00:00:05"
    }
  }
}
```

### 4. Program.cs Configuration

```csharp
builder.Host.UseWolverine(opts =>
{
    // Enable SQL Server persistence
    opts.PersistMessagesWithSqlServer(
        builder.Configuration.GetConnectionString("WolverineDatabase"));
    
    // Enable durability agent
    opts.Durability.DurabilityAgentEnabled = true;
    
    // Configure polling intervals
    opts.Durability.ScheduledJobPollingTime = TimeSpan.FromSeconds(5);
    
    // Make local queues durable
    opts.LocalQueue("important").UseDurableInbox();
});

// Create database schema on startup
await app.EnsureWolverineTablesExistAsync();
```

### 5. Database Initialization

The schema is automatically created on first run, but you can also:

```bash
# View the SQL schema
curl http://localhost:5100/admin/wolverine/schema

# Or generate manually
dotnet run -- wolverine-schema --output schema.sql
```

## 📊 Database Schema

Wolverine creates the following tables:

### wolverine_outgoing_envelopes (Outbox)
Stores messages to be sent:
```sql
CREATE TABLE wolverine.wolverine_outgoing_envelopes (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    owner_id INT NOT NULL,
    destination NVARCHAR(250) NOT NULL,
    deliver_by DATETIME2,
    body VARBINARY(MAX) NOT NULL,
    attempts INT DEFAULT 0,
    message_type NVARCHAR(250) NOT NULL
)
```

### wolverine_incoming_envelopes (Inbox)
Tracks processed messages for deduplication:
```sql
CREATE TABLE wolverine.wolverine_incoming_envelopes (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    status NVARCHAR(25) NOT NULL,
    owner_id INT NOT NULL,
    execution_time DATETIME2,
    attempts INT DEFAULT 0,
    body VARBINARY(MAX) NOT NULL,
    message_type NVARCHAR(250) NOT NULL
)
```

### wolverine_scheduled_jobs
Stores scheduled/delayed messages:
```sql
CREATE TABLE wolverine.wolverine_scheduled_jobs (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    execution_time DATETIME2 NOT NULL,
    attempts INT DEFAULT 0,
    body VARBINARY(MAX) NOT NULL,
    message_type NVARCHAR(250) NOT NULL
)
```

### wolverine_node_assignments
Tracks node ownership for distributed scenarios:
```sql
CREATE TABLE wolverine.wolverine_node_assignments (
    id INT PRIMARY KEY,
    node_number INT NOT NULL,
    description NVARCHAR(500)
)
```

## 💡 Usage Patterns

### Pattern 1: Transactional Outbox

```csharp
public class CreateOrderHandler
{
    [WolverineHandler]
    [Transactional] // Ensures atomicity
    public async Task Handle(CreateOrderCommand command, 
        ApplicationDbContext db, 
        IMessageBus bus)
    {
        // Save to database
        var order = new Order { Id = Guid.NewGuid(), ... };
        db.Orders.Add(order);
        
        // Publish event - stored in outbox table
        await bus.PublishAsync(new OrderCreatedEvent(order.Id));
        
        // Both saved atomically in same transaction
        await db.SaveChangesAsync();
        
        // Message will be sent by background process
    }
}
```

### Pattern 2: Scheduled Messages

```csharp
public class ScheduleReminderHandler
{
    public async Task Handle(ScheduleReminderCommand command, IMessageBus bus)
    {
        // Schedule message for future delivery
        await bus.ScheduleAsync(
            new SendReminderCommand(command.UserId),
            command.SendAt); // DateTime in future
            
        // Message stored in wolverine_scheduled_jobs table
    }
}
```

### Pattern 3: Delayed Retry

```csharp
[WolverineHandler]
[RetryOn(typeof(HttpRequestException), 1000, 2000, 5000)]
[MaximumAttempts(3)]
public async Task Handle(SendEmailCommand command)
{
    // If this fails, Wolverine will retry with delays: 1s, 2s, 5s
    await _emailService.SendAsync(command.To, command.Body);
}
```

### Pattern 4: Local Durable Queues

```csharp
// Configuration
opts.LocalQueue("high-priority").UseDurableInbox();

// Usage
await bus.PublishAsync(
    new ProcessPaymentCommand(orderId),
    new DeliveryOptions { Queue = "high-priority" });
```

### Pattern 5: Saga/Workflow

```csharp
public class OrderSaga
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(StartOrderCommand cmd, IMessageBus bus)
    {
        // Step 1: Validate inventory
        await bus.PublishAsync(new ValidateInventoryCommand(cmd.OrderId));
        
        // Step 2: Reserve payment
        await bus.PublishAsync(new ReservePaymentCommand(cmd.OrderId));
        
        // Both messages stored in outbox atomically
    }
    
    [WolverineHandler]
    public async Task Handle(InventoryValidatedEvent evt, IMessageBus bus)
    {
        // Continue saga based on event
        await bus.PublishAsync(new ShipOrderCommand(evt.OrderId));
    }
}
```

## 📈 Monitoring

### Health Checks

```bash
# Overall health
curl http://localhost:5100/health

# Wolverine-specific health
curl http://localhost:5100/health/wolverine
```

Response:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "wolverine-sqlserver",
      "status": "Healthy",
      "description": "SQL Server connection is healthy"
    },
    {
      "name": "wolverine-messaging",
      "status": "Healthy",
      "data": {
        "incoming_messages": 0,
        "outgoing_messages": 2,
        "scheduled_messages": 5,
        "total_messages": 7
      }
    }
  ]
}
```

### Query Message Status

```sql
-- Pending outgoing messages
SELECT id, message_type, attempts, owner_id, destination
FROM wolverine.wolverine_outgoing_envelopes
WHERE owner_id = 0; -- Not yet claimed

-- Scheduled jobs
SELECT id, message_type, execution_time
FROM wolverine.wolverine_scheduled_jobs
WHERE execution_time > GETUTCDATE();

-- Failed messages (multiple attempts)
SELECT id, message_type, attempts, status
FROM wolverine.wolverine_incoming_envelopes
WHERE attempts > 1;
```

### Metrics Endpoint

```csharp
// Add to your API
app.MapGet("/admin/wolverine/stats", async (IWolverineRuntime runtime) =>
{
    var admin = runtime.Storage.Database.Admin;
    return new
    {
        Incoming = await admin.GetIncomingEnvelopeCountAsync(),
        Outgoing = await admin.GetOutgoingEnvelopeCountAsync(),
        Scheduled = await admin.GetScheduledEnvelopeCountAsync()
    };
});
```

## 🐛 Troubleshooting

### Messages Not Being Sent

**Problem**: Messages stuck in outbox table

**Solutions**:
1. Check durability agent is enabled:
   ```csharp
   opts.Durability.DurabilityAgentEnabled = true;
   ```

2. Verify polling interval:
   ```csharp
   opts.Durability.ScheduledJobPollingTime = TimeSpan.FromSeconds(5);
   ```

3. Check logs for errors:
   ```bash
   docker logs wolverine-api
   ```

4. Query outbox table:
   ```sql
   SELECT * FROM wolverine.wolverine_outgoing_envelopes;
   ```

### Duplicate Message Processing

**Problem**: Same message processed multiple times

**Solution**: Ensure inbox is enabled:
```csharp
opts.LocalQueue("queue-name").UseDurableInbox();
```

### Schema Creation Fails

**Problem**: Tables not created on startup

**Solutions**:
1. Check SQL Server permissions
2. Manually create schema:
   ```csharp
   await runtime.Storage.Database.EnsureStorageExistsAsync(typeof(Program));
   ```

3. Use migration script:
   ```bash
   dotnet run -- wolverine-schema --output schema.sql
   sqlcmd -S localhost -U sa -P YourPassword -i schema.sql
   ```

### High Message Count

**Problem**: Too many pending messages in database

**Solutions**:
1. Increase polling frequency:
   ```csharp
   opts.Durability.ScheduledJobPollingTime = TimeSpan.FromSeconds(1);
   ```

2. Scale out with multiple nodes (node assignment)

3. Check for failing handlers causing retries

4. Clear old messages:
   ```sql
   -- Clear processed messages older than 7 days
   DELETE FROM wolverine.wolverine_incoming_envelopes
   WHERE status = 'Handled' AND execution_time < DATEADD(day, -7, GETUTCDATE());
   ```

### Connection Pool Exhaustion

**Problem**: "Timeout expired" errors

**Solution**: Increase connection pool size:
```json
{
  "ConnectionStrings": {
    "WolverineDatabase": "Server=localhost;Database=WolverineDb;User Id=sa;Password=Pass;Min Pool Size=10;Max Pool Size=100;"
  }
}
```

## 📚 Additional Resources

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [SQL Server Persistence](https://wolverine.netlify.app/guide/durability/sqlserver.html)
- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Message Deduplication](https://wolverine.netlify.app/guide/durability/inbox.html)

## 🔍 Quick Reference

| Feature | Configuration | Default |
|---------|--------------|---------|
| Polling Interval | `ScheduledJobPollingTime` | 5 seconds |
| Node Reassignment | `NodeReassignmentPollingTime` | 1 minute |
| Message Retention | Automatic cleanup | 3 days |
| Max Attempts | `[MaximumAttempts(n)]` | 3 |
| Schema Name | `opts.SchemaName` | "wolverine" |

## ✅ Checklist

- [ ] SQL Server is running and accessible
- [ ] Connection string is configured
- [ ] Durability agent is enabled
- [ ] Schema tables are created
- [ ] Health checks are passing
- [ ] Handlers are registered with `[WolverineHandler]`
- [ ] `[Transactional]` attribute is used where needed
- [ ] Monitoring endpoints are configured
- [ ] Retry policies are defined for external services
