# Wolverine SQL Server Transport Migration Guide

This document describes the changes made to implement Wolverine's transactional inbox/outbox pattern using SQL Server transport in the E470 Audit Log solution.

## Overview of Changes

The solution has been updated to implement a complete producer-consumer messaging pattern using Wolverine with SQL Server as the message transport and storage mechanism.

## What is Wolverine?

Wolverine is a next-generation .NET messaging framework that provides:
- **Message-based architecture** for building distributed systems
- **Transactional outbox pattern** - ensures messages are published reliably
- **Transactional inbox pattern** - ensures messages are processed exactly once
- **Automatic retry and error handling**
- **Integration with Entity Framework Core** for transactional consistency

## Key Concepts

### Transactional Outbox Pattern

**Problem**: When publishing a message after saving data to database, the application might crash after the database commit but before the message is sent, resulting in data inconsistency.

**Solution**: 
1. Save application data and message to outbox table in same transaction
2. Background worker reads from outbox and publishes messages
3. Message is deleted from outbox after successful delivery

### Transactional Inbox Pattern

**Problem**: When processing a message, the handler might fail after processing but before acknowledging, causing duplicate processing.

**Solution**:
1. Store incoming message in inbox table
2. Process message within a database transaction
3. Mark message as processed in same transaction
4. If processing fails, message remains in inbox for retry

## Changes Made

### 1. New Projects and Files

#### E470.AuditLog.Contracts (New Project)
- **Purpose**: Shared message contracts and domain models
- **Files**:
  - `Messages/AuditLogCreated.cs` - Message contract for audit log events
  - `Models/AuditLogEntry.cs` - Domain entity for audit logs

#### Producer Project Updates
- **New Files**:
  - `Data/AuditLogDbContext.cs` - EF Core database context
  - `Controllers/AuditLogController.cs` - API for creating and querying audit logs

#### Consumer Project Updates
- **New Files**:
  - `Data/AuditLogDbContext.cs` - EF Core database context (consumer side)
  - `Handlers/AuditLogCreatedHandler.cs` - Message handler for processing audit logs
  - `Controllers/AuditLogQueryController.cs` - API for querying processed audit logs

### 2. Updated Configuration Files

#### Producer - E470.AuditLog.Produser.csproj

**Added Package References**:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
```

**Added Project Reference**:
```xml
<ProjectReference Include="..\E470.AuditLog.Contracts\E470.AuditLog.Contracts.csproj" />
```

#### Consumer - E470.AuditLog.Consumer.csproj

**Added Package References**:
```xml
<PackageReference Include="WolverineFx" Version="5.16.2" />
<PackageReference Include="WolverineFx.EntityFrameworkCore" Version="5.16.2" />
<PackageReference Include="WolverineFx.SqlServer" Version="5.16.2" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
```

**Added Project Reference**:
```xml
<ProjectReference Include="..\E470.AuditLog.Contracts\E470.AuditLog.Contracts.csproj" />
```

### 3. Program.cs Updates

#### Producer - Before
```csharp
var connectionString = builder.Configuration.GetConnectionString("Alerts")!;

builder.Host.UseWolverine(opts =>
{
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.UseDurableLocalQueues();
});
```

#### Producer - After
```csharp
var connectionString = builder.Configuration.GetConnectionString("message-bus-mssql")!;

// Register DbContext
builder.Services.AddDbContext<AuditLogDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Host.UseWolverine(opts =>
{
    // SQL Server message persistence
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");

    // EF Core transaction integration
    opts.UseEntityFrameworkCoreTransactions();

    // Durable messaging policies
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableOutbox();

    // Message routing
    opts.PublishMessage<AuditLogCreated>().ToLocalQueue("auditlog");
});
```

**Key Changes**:
1. ✅ Fixed connection string to match AppHost configuration
2. ✅ Added DbContext registration
3. ✅ Added UseDurableOutbox policy
4. ✅ Configured message routing to local queue

#### Consumer - Before
```csharp
// No Wolverine configuration
```

#### Consumer - After
```csharp
var connectionString = builder.Configuration.GetConnectionString("message-bus-mssql")!;

// Register DbContext
builder.Services.AddDbContext<AuditLogDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Host.UseWolverine(opts =>
{
    // SQL Server message persistence
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");

    // EF Core transaction integration
    opts.UseEntityFrameworkCoreTransactions();

    // Durable messaging policies
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableInbox();

    // Listen to local queue
    opts.ListenToLocalQueue("auditlog");
});
```

**Key Changes**:
1. ✅ Added complete Wolverine configuration
2. ✅ Added DbContext registration
3. ✅ Configured SQL Server transport
4. ✅ Added inbox/outbox policies
5. ✅ Configured local queue listener

### 4. Database Schema

Wolverine automatically creates these tables in SQL Server:

#### Wolverine Schema Tables

**wolverine.wolverine_outgoing_messages**
- Stores messages to be sent (outbox pattern)
- Columns: id, message_type, serialized_message, status, destination, attempts, etc.

**wolverine.wolverine_incoming_messages**
- Stores messages being processed (inbox pattern)
- Columns: id, message_type, serialized_message, status, received_at, attempts, etc.

**wolverine.wolverine_dead_letters**
- Stores failed messages after max retries
- Columns: id, message_type, exception_type, exception_message, received_at, etc.

#### Application Tables

**dbo.AuditLogEntries**
- Stores audit log entries
- Used by both Producer and Consumer with different purposes:
  - Producer: Stores created audit logs
  - Consumer: Stores processed audit logs

## Message Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Producer Service                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. API Request (POST /api/auditlog)                               │
│      ↓                                                              │
│  2. AuditLogController                                             │
│      ↓                                                              │
│  3. Save to DbContext (AuditLogEntry)                              │
│      ↓                                                              │
│  4. Publish Message (AuditLogCreated)                              │
│      ↓                                                              │
│  ┌──────────────────────────────────┐                              │
│  │  Transaction Scope               │                              │
│  │  ┌────────────────────────────┐  │                              │
│  │  │ INSERT AuditLogEntry       │  │                              │
│  │  │ INSERT wolverine_outgoing  │  │                              │
│  │  └────────────────────────────┘  │                              │
│  │  COMMIT                          │                              │
│  └──────────────────────────────────┘                              │
│      ↓                                                              │
│  5. Wolverine Background Worker                                    │
│      ↓                                                              │
│  6. Read from outbox table                                         │
│      ↓                                                              │
│  7. Send message to Consumer queue                                 │
│      ↓                                                              │
│  8. Delete from outbox on success                                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │  Message
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                          Consumer Service                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. Message arrives at local queue                                 │
│      ↓                                                              │
│  2. Wolverine writes to inbox table                                │
│      ↓                                                              │
│  3. AuditLogCreatedHandler invoked                                 │
│      ↓                                                              │
│  4. Check for duplicate (idempotency)                              │
│      ↓                                                              │
│  5. Create AuditLogEntry                                           │
│      ↓                                                              │
│  ┌──────────────────────────────────┐                              │
│  │  Transaction Scope               │                              │
│  │  ┌────────────────────────────┐  │                              │
│  │  │ INSERT AuditLogEntry       │  │                              │
│  │  │ UPDATE wolverine_incoming  │  │                              │
│  │  │   (mark as processed)      │  │                              │
│  │  └────────────────────────────┘  │                              │
│  │  COMMIT                          │                              │
│  └──────────────────────────────────┘                              │
│      ↓                                                              │
│  6. Message removed from inbox                                     │
│      ↓                                                              │
│  7. Available via Query API                                        │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Benefits of This Architecture

### 1. **Reliability**
- Messages are never lost due to application crashes
- Automatic retry with exponential backoff
- Dead letter queue for failed messages

### 2. **Consistency**
- Database operations and message publishing in same transaction
- Exactly-once message processing semantics
- No partial failures or data inconsistencies

### 3. **Scalability**
- Asynchronous message processing
- Easy to scale consumers horizontally
- Built-in message queuing and load distribution

### 4. **Maintainability**
- Clear separation of concerns
- Testable message handlers
- Observable via SQL queries and monitoring tools

### 5. **Fault Tolerance**
- Automatic retry on failures
- Dead letter queue for manual intervention
- Message durability survives application restarts

## Migration Checklist

If migrating an existing application to this pattern:

- [ ] Install Wolverine NuGet packages
- [ ] Install Entity Framework Core packages
- [ ] Create message contracts (immutable records)
- [ ] Create DbContext with proper entity configuration
- [ ] Update Program.cs with Wolverine configuration
- [ ] Implement message handlers with [Transactional] attribute
- [ ] Add idempotency checks in handlers
- [ ] Configure connection strings
- [ ] Test message flow end-to-end
- [ ] Set up monitoring for dead letters
- [ ] Configure retry policies if needed
- [ ] Deploy database schema changes

## Testing Strategy

### Unit Tests
```csharp
// Test message handler logic
[Fact]
public async Task Handler_Should_Process_Message_Successfully()
{
    // Arrange
    var message = new AuditLogCreated { /* ... */ };
    var handler = new AuditLogCreatedHandler(logger);
    
    // Act
    await handler.Handle(message, dbContext, CancellationToken.None);
    
    // Assert
    var entry = await dbContext.AuditLogEntries.FindAsync(message.Id);
    Assert.NotNull(entry);
    Assert.True(entry.IsProcessed);
}
```

### Integration Tests
```csharp
// Test end-to-end message flow
[Fact]
public async Task Should_Publish_And_Consume_Message()
{
    // Arrange
    var request = new CreateAuditLogRequest { /* ... */ };
    
    // Act - Publish via API
    var response = await producerClient.PostAsJsonAsync("/api/auditlog", request);
    var result = await response.Content.ReadFromJsonAsync<CreateResult>();
    
    // Wait for processing
    await Task.Delay(TimeSpan.FromSeconds(2));
    
    // Assert - Check consumer processed it
    var consumerResponse = await consumerClient.GetAsync($"/api/auditlogquery/{result.Id}");
    var processedEntry = await consumerResponse.Content.ReadFromJsonAsync<AuditLogEntry>();
    Assert.True(processedEntry.IsProcessed);
}
```

## Monitoring and Troubleshooting

### Check Message Status

```sql
-- Check outbox (pending messages to send)
SELECT TOP 100 * 
FROM wolverine.wolverine_outgoing_messages 
ORDER BY id DESC;

-- Check inbox (messages being processed)
SELECT TOP 100 * 
FROM wolverine.wolverine_incoming_messages 
ORDER BY id DESC;

-- Check dead letters (failed messages)
SELECT TOP 100 * 
FROM wolverine.wolverine_dead_letters 
ORDER BY received_at DESC;
```

### Common Issues

#### Messages stuck in outbox
- Check Producer application logs
- Verify SQL Server connectivity
- Check destination queue configuration

#### Messages stuck in inbox
- Check Consumer application logs
- Verify handler is registered and found
- Check for exceptions in handler execution

#### Messages in dead letter queue
- Review exception_message column
- Fix handler logic or data issue
- Manually reprocess by inserting back to incoming table

## Performance Considerations

### Database Indexes
Wolverine creates appropriate indexes automatically, but monitor query performance:
```sql
-- Check index usage
SELECT * FROM sys.dm_db_index_usage_stats
WHERE database_id = DB_ID('message-bus-mssql')
AND OBJECT_ID IN (
    OBJECT_ID('wolverine.wolverine_outgoing_messages'),
    OBJECT_ID('wolverine.wolverine_incoming_messages')
);
```

### Message Throughput
- Default: ~1000 messages/second per consumer instance
- Scale horizontally by adding more consumer instances
- Configure max concurrent messages if needed

### Database Size
- Monitor table sizes regularly
- Wolverine cleans up processed messages automatically
- Consider archiving dead letters periodically

## Security Considerations

1. **Connection Strings**: Use user secrets or Azure Key Vault in production
2. **Database Access**: Use least-privilege SQL accounts
3. **Message Content**: Consider encrypting sensitive data in messages
4. **Network**: Use encrypted connections (TrustServerCertificate=False in production)

## Deployment Checklist

- [ ] Update connection strings for target environment
- [ ] Run database initialization script
- [ ] Deploy Producer service
- [ ] Deploy Consumer service
- [ ] Verify Wolverine tables are created
- [ ] Test message flow with sample data
- [ ] Set up monitoring alerts for dead letters
- [ ] Document operational procedures
- [ ] Train operations team on troubleshooting

## Rollback Plan

If issues occur after deployment:

1. **Immediate**: Stop Consumer service to prevent message processing
2. **Preserve**: Messages remain in inbox/outbox tables
3. **Rollback**: Deploy previous version of applications
4. **Resume**: Start services - messages will be processed from tables
5. **Investigate**: Review logs and dead letters for root cause

## Further Resources

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)

## Support

For questions or issues:
1. Check Wolverine GitHub issues
2. Review application logs in Aspire dashboard
3. Query Wolverine tables for message status
4. Check dead letter queue for errors
