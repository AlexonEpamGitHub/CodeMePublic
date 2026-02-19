# Troubleshooting Guide - Wolverine SQL Server Transport

This guide helps you diagnose and resolve common issues with the Wolverine SQL Server transport implementation.

## Table of Contents

1. [Connection Issues](#connection-issues)
2. [Message Processing Issues](#message-processing-issues)
3. [Database Issues](#database-issues)
4. [Performance Issues](#performance-issues)
5. [Configuration Issues](#configuration-issues)
6. [Docker Issues](#docker-issues)
7. [Debugging Techniques](#debugging-techniques)

---

## Connection Issues

### Issue: "Connection refused" or "Cannot connect to SQL Server"

**Symptoms:**
- Application fails to start
- Error: "A network-related or instance-specific error occurred"

**Diagnosis:**
```bash
# Check if SQL Server container is running
docker ps | grep sql

# Check container logs
docker logs <sql-container-id>
```

**Solutions:**

1. **Wait for SQL Server initialization** (30-60 seconds after container start)
   ```bash
   # Watch container logs
   docker logs <sql-container-id> -f
   # Wait for: "SQL Server is now ready for client connections"
   ```

2. **Verify connection string**
   ```csharp
   // In Program.cs - should be:
   var connectionString = builder.Configuration.GetConnectionString("message-bus-mssql")!;
   ```

3. **Check Docker Desktop**
   - Ensure Docker Desktop is running
   - Check available memory (minimum 2GB for SQL Server)

4. **Verify port availability**
   ```bash
   # Check if port 5051 is free
   netstat -an | grep 5051
   ```

---

### Issue: "Login failed for user 'sa'"

**Symptoms:**
- Container is running but connection fails
- Error: "Login failed"

**Solutions:**

1. **Check password is set correctly**
   ```bash
   cd src/E470.AuditLog.Produser.AppHost
   dotnet user-secrets list
   # Should show: Parameters:sql-password = <your-password>
   ```

2. **Password must meet SQL Server requirements:**
   - At least 8 characters
   - Contains uppercase, lowercase, numbers, and symbols
   - Example: `MyP@ssw0rd123!`

3. **Reset user secret**
   ```bash
   dotnet user-secrets remove "Parameters:sql-password"
   dotnet user-secrets set "Parameters:sql-password" "NewP@ssw0rd123!"
   ```

---

## Message Processing Issues

### Issue: Messages not being delivered from Producer to Consumer

**Symptoms:**
- Producer API works
- Messages stay in outbox table
- Consumer never receives messages

**Diagnosis:**
```sql
-- Check outbox
SELECT 
    id, 
    message_type, 
    status, 
    attempts,
    destination
FROM wolverine.wolverine_outgoing_messages
ORDER BY id DESC;

-- Should show status and attempts
```

**Solutions:**

1. **Check queue name matches**
   ```csharp
   // Producer (Program.cs)
   opts.PublishMessage<AuditLogCreated>()
       .ToLocalQueue("auditlog");  // ← Must match
   
   // Consumer (Program.cs)
   opts.ListenToLocalQueue("auditlog");  // ← Must match
   ```

2. **Verify Consumer is running**
   - Check Aspire Dashboard
   - Look for "e470-auditlog-consumer" in services

3. **Check for errors in Consumer logs**
   ```bash
   # In Aspire Dashboard → Logs → Filter by Consumer
   ```

4. **Verify Wolverine background worker is running**
   ```csharp
   // Should be enabled by default
   opts.Policies.UseDurableOutbox();
   ```

---

### Issue: Messages in dead letter queue

**Symptoms:**
- Messages not processed successfully
- Found in `wolverine_dead_letters` table

**Diagnosis:**
```sql
-- Check dead letters
SELECT 
    id,
    message_type,
    exception_type,
    exception_message,
    attempts,
    received_at
FROM wolverine.wolverine_dead_letters
ORDER BY received_at DESC;
```

**Solutions:**

1. **Review exception message**
   - Look at `exception_message` column
   - Fix the underlying issue in handler code

2. **Check handler implementation**
   ```csharp
   // Ensure handler is async and uses await
   public async Task Handle(AuditLogCreated message, AuditLogDbContext dbContext)
   {
       // Handler logic with proper error handling
   }
   ```

3. **Verify database schema**
   ```sql
   -- Ensure table exists
   SELECT * FROM INFORMATION_SCHEMA.TABLES 
   WHERE TABLE_NAME = 'AuditLogEntries';
   ```

4. **Reprocess dead letter**
   ```sql
   -- Move back to inbox for retry
   INSERT INTO wolverine.wolverine_incoming_messages 
       (message_type, serialized_message, status, owner_id, received_at, attempts, body)
   SELECT 
       message_type, 
       serialized_message, 
       'Scheduled' as status,
       0 as owner_id,
       GETUTCDATE() as received_at,
       0 as attempts,
       body
   FROM wolverine.wolverine_dead_letters
   WHERE id = <dead-letter-id>;
   
   -- Delete from dead letters
   DELETE FROM wolverine.wolverine_dead_letters WHERE id = <dead-letter-id>;
   ```

---

### Issue: Duplicate message processing

**Symptoms:**
- Same message processed multiple times
- Duplicate entries in database

**Diagnosis:**
```sql
-- Check for duplicates
SELECT Id, COUNT(*) as Count
FROM dbo.AuditLogEntries
GROUP BY Id
HAVING COUNT(*) > 1;
```

**Solutions:**

1. **Ensure idempotency check is in place**
   ```csharp
   public async Task Handle(AuditLogCreated message, AuditLogDbContext dbContext)
   {
       // Check for existing entry
       var existingEntry = await dbContext.AuditLogEntries
           .FirstOrDefaultAsync(e => e.Id == message.Id);
       
       if (existingEntry != null)
       {
           _logger.LogWarning("Already processed message {Id}", message.Id);
           return; // Skip processing
       }
       
       // Process message...
   }
   ```

2. **Verify [Transactional] attribute is present**
   ```csharp
   [Transactional]
   public async Task Handle(...)
   ```

3. **Check transaction configuration**
   ```csharp
   opts.UseEntityFrameworkCoreTransactions();
   ```

---

## Database Issues

### Issue: Wolverine tables not created

**Symptoms:**
- Error: "Invalid object name 'wolverine.wolverine_outgoing_messages'"

**Solutions:**

1. **Run initialization script**
   ```bash
   # Execute src/database/init-wolverine-db.sql
   ```

2. **Let Wolverine auto-create tables**
   - Start the application
   - Wolverine will create tables on first run
   - Check logs for "Creating message storage"

3. **Verify schema exists**
   ```sql
   -- Check schema
   SELECT * FROM sys.schemas WHERE name = 'wolverine';
   
   -- If not exists, create it
   CREATE SCHEMA wolverine;
   ```

---

### Issue: Application tables not created

**Symptoms:**
- Error: "Invalid object name 'dbo.AuditLogEntries'"

**Solutions:**

1. **Run SQL script manually**
   ```sql
   CREATE TABLE [dbo].[AuditLogEntries](
       [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
       [Action] [nvarchar](100) NOT NULL,
       [UserId] [nvarchar](100) NOT NULL,
       [Details] [nvarchar](2000) NULL,
       [Timestamp] [datetime2](7) NOT NULL,
       [IpAddress] [nvarchar](50) NULL,
       [Resource] [nvarchar](500) NULL,
       [IsProcessed] [bit] NOT NULL DEFAULT 0,
       [ProcessedAt] [datetime2](7) NULL
   );
   ```

2. **Add EF Core migrations** (Future enhancement)
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

---

### Issue: "Database 'message-bus-mssql' does not exist"

**Symptoms:**
- Connection string correct
- Database not found

**Solutions:**

1. **Check AppHost creates database**
   ```csharp
   // In AppHost.cs
   var dbMessageBus = sqlServerMessageBus.AddDatabase("message-bus-mssql");
   ```

2. **Manually create database**
   ```sql
   CREATE DATABASE [message-bus-mssql];
   ```

3. **Restart Aspire AppHost**
   - Stop the application (Ctrl+C)
   - Run `dotnet run` again

---

## Performance Issues

### Issue: Slow message processing

**Symptoms:**
- Messages taking too long to process
- High latency between publish and consumption

**Diagnosis:**
```sql
-- Check message processing times
SELECT 
    message_type,
    COUNT(*) as total,
    AVG(attempts) as avg_attempts
FROM wolverine.wolverine_incoming_messages
GROUP BY message_type;
```

**Solutions:**

1. **Increase parallel processing**
   ```csharp
   opts.ListenToLocalQueue("auditlog")
       .MaximumParallelMessages(20);  // Increase from 10
   ```

2. **Optimize database queries**
   ```csharp
   // Use indexes
   modelBuilder.Entity<AuditLogEntry>()
       .HasIndex(e => e.Timestamp);
   ```

3. **Profile handler code**
   - Add timing logs
   - Identify slow operations
   - Optimize database queries

4. **Check database performance**
   ```sql
   -- Check for blocking
   SELECT * FROM sys.dm_exec_requests
   WHERE blocking_session_id <> 0;
   ```

---

### Issue: High memory usage

**Symptoms:**
- Application memory grows over time
- Out of memory exceptions

**Solutions:**

1. **Configure durability agent worker count**
   ```csharp
   opts.Durability.DurabilityAgentWorkerCount = 2;
   ```

2. **Adjust polling interval**
   ```csharp
   opts.Durability.ScheduledJobPollingTime = TimeSpan.FromSeconds(10);
   ```

3. **Limit recovery batch size**
   ```csharp
   opts.Durability.RecoveryBatchSize = 100;
   ```

4. **Ensure proper DbContext disposal**
   ```csharp
   // Use dependency injection - automatic disposal
   public async Task Handle(
       AuditLogCreated message, 
       AuditLogDbContext dbContext)  // ← Automatically disposed
   {
       // Handler logic
   }
   ```

---

## Configuration Issues

### Issue: "Connection string not found"

**Symptoms:**
- Error: "Value cannot be null. (Parameter 'connectionString')"

**Solutions:**

1. **Verify AppHost references database**
   ```csharp
   builder.AddProject<Projects.E470_AuditLog_Produser>("e470-auditlog-produser")
       .WithReference(dbMessageBus)  // ← Must be present
       .WaitFor(dbMessageBus);
   ```

2. **Check connection string name**
   ```csharp
   // Must match database name in AppHost
   var connectionString = builder.Configuration
       .GetConnectionString("message-bus-mssql")!;
   ```

---

### Issue: Handler not discovered

**Symptoms:**
- Messages stay in inbox
- No handler execution logs

**Solutions:**

1. **Verify handler is in correct namespace**
   ```csharp
   // Should be in: E470.AuditLog.Consumer.Handlers
   namespace E470.AuditLog.Consumer.Handlers;
   ```

2. **Enable auto-discovery**
   ```csharp
   opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
   ```

3. **Check handler method signature**
   ```csharp
   // Must match message type
   public async Task Handle(AuditLogCreated message, ...)
   ```

4. **Enable verbose logging**
   ```json
   // appsettings.json
   {
     "Logging": {
       "LogLevel": {
         "Wolverine": "Debug"
       }
     }
   }
   ```

---

## Docker Issues

### Issue: SQL Server container keeps restarting

**Symptoms:**
- Container status shows "Restarting"
- Application cannot connect

**Diagnosis:**
```bash
docker ps -a | grep sql
docker logs <container-id>
```

**Solutions:**

1. **Insufficient memory**
   - SQL Server requires minimum 2GB
   - Increase Docker Desktop memory limit
   - Settings → Resources → Memory → 4GB+

2. **Check password complexity**
   - Must meet SQL Server requirements
   - Use strong password with special characters

3. **Port conflict**
   ```bash
   # Check if port 5051 is in use
   lsof -i :5051  # macOS/Linux
   netstat -ano | findstr :5051  # Windows
   ```

---

### Issue: Cannot remove old containers

**Symptoms:**
- Error: "Container is already in use"

**Solutions:**
```bash
# Stop all related containers
docker stop $(docker ps -a | grep sql | awk '{print $1}')

# Remove containers
docker rm $(docker ps -a | grep sql | awk '{print $1}')

# Remove volumes
docker volume ls
docker volume rm <volume-name>

# Restart application
```

---

## Debugging Techniques

### Enable Detailed Logging

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Wolverine": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Inspect Message Flow

```sql
-- Producer: Check outbox
SELECT TOP 10 
    id,
    message_type,
    status,
    destination,
    attempts,
    deliver_by
FROM wolverine.wolverine_outgoing_messages
ORDER BY id DESC;

-- Consumer: Check inbox
SELECT TOP 10
    id,
    message_type,
    status,
    owner_id,
    received_at,
    attempts
FROM wolverine.wolverine_incoming_messages
ORDER BY id DESC;

-- Check dead letters
SELECT TOP 10
    id,
    message_type,
    exception_type,
    exception_message,
    received_at
FROM wolverine.wolverine_dead_letters
ORDER BY received_at DESC;
```

### Monitor with Aspire Dashboard

1. **View Real-time Logs**
   - Logs tab → Filter by service
   - Look for errors and warnings

2. **Trace Message Flow**
   - Traces tab → Find request
   - Follow distributed trace

3. **Check Service Health**
   - Resources tab → View service status
   - Monitor CPU/Memory usage

### Debug Handler Execution

```csharp
public class AuditLogCreatedHandler
{
    private readonly ILogger<AuditLogCreatedHandler> _logger;
    
    [Transactional]
    public async Task Handle(AuditLogCreated message, AuditLogDbContext dbContext)
    {
        _logger.LogInformation("=== Handler Started ===");
        _logger.LogInformation("Message ID: {Id}", message.Id);
        _logger.LogInformation("Action: {Action}", message.Action);
        
        try
        {
            // Check for duplicates
            var exists = await dbContext.AuditLogEntries
                .AnyAsync(e => e.Id == message.Id);
            
            _logger.LogInformation("Entry exists: {Exists}", exists);
            
            if (exists)
            {
                _logger.LogWarning("Duplicate detected, skipping");
                return;
            }
            
            // Process message
            var entry = new AuditLogEntry { /* ... */ };
            dbContext.AuditLogEntries.Add(entry);
            
            _logger.LogInformation("Saving to database...");
            await dbContext.SaveChangesAsync();
            
            _logger.LogInformation("=== Handler Completed Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== Handler Failed ===");
            throw;
        }
    }
}
```

### SQL Server Profiler

For advanced debugging:
```sql
-- Enable query execution statistics
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

-- Run queries to see performance
SELECT * FROM dbo.AuditLogEntries;
```

### Use Breakpoints

In Visual Studio:
1. Set breakpoints in handler code
2. Attach to process if needed
3. Step through execution
4. Inspect variables and DbContext state

---

## Common Error Messages

### "The ConnectionString property has not been initialized"

**Solution:** Verify connection string configuration in Program.cs

### "Cannot insert duplicate key"

**Solution:** Check idempotency logic in handler

### "Execution Timeout Expired"

**Solution:** Increase command timeout or optimize queries

### "A transaction is required"

**Solution:** Add `[Transactional]` attribute to handler

### "No handler found for message type"

**Solution:** Ensure handler is in correct namespace and assembly is scanned

---

## Getting Help

If you're still stuck:

1. **Check Documentation**
   - [Wolverine Docs](https://wolverine.netlify.app/)
   - [.NET Aspire Docs](https://learn.microsoft.com/en-us/dotnet/aspire/)

2. **Review Logs**
   - Application logs in Aspire Dashboard
   - SQL Server logs: `docker logs <container-id>`

3. **Query Message State**
   - Check Wolverine tables in SQL Server
   - Verify message flow step by step

4. **Community Resources**
   - Wolverine GitHub Issues
   - Stack Overflow
   - .NET Discord/Slack channels

---

## Preventive Measures

### Best Practices

1. ✅ **Always use [Transactional] attribute**
2. ✅ **Implement idempotency checks**
3. ✅ **Use structured logging**
4. ✅ **Monitor dead letter queue**
5. ✅ **Set up health checks**
6. ✅ **Configure appropriate timeouts**
7. ✅ **Test failure scenarios**
8. ✅ **Review Wolverine tables regularly**

### Monitoring Checklist

- [ ] Set up alerts for dead letters
- [ ] Monitor message processing latency
- [ ] Track message throughput
- [ ] Monitor database size growth
- [ ] Check for stuck messages
- [ ] Review error logs daily

---

**Remember:** Most issues are related to configuration or connection strings. Always double-check these first!

For more information, see the [README](../src/README.md) and [Migration Guide](./MIGRATION_GUIDE.md).
