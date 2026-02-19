# 🚀 Quick Start: Wolverine with SQL Server Persistence

Get up and running with Wolverine's transactional inbox/outbox in 5 minutes!

## Prerequisites

- .NET 8 SDK
- Docker (for SQL Server)
- Your favorite IDE

## Step 1: Start SQL Server (30 seconds)

```bash
cd audit-log/src/Web.Api.Wolverine
docker-compose up -d sqlserver
```

Wait for SQL Server to be ready:
```bash
# Check health
docker logs wolverine-sqlserver
```

## Step 2: Run the Application (30 seconds)

```bash
dotnet run
```

The application will:
1. ✅ Connect to SQL Server
2. ✅ Create Wolverine database schema automatically
3. ✅ Start the durability agent
4. ✅ Begin processing messages

## Step 3: Verify Setup (1 minute)

### Check Health Status
```bash
curl http://localhost:5100/health/wolverine
```

Expected response:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "wolverine-sqlserver",
      "status": "Healthy"
    },
    {
      "name": "wolverine-messaging",
      "status": "Healthy",
      "data": {
        "incoming_messages": 0,
        "outgoing_messages": 0,
        "scheduled_messages": 0
      }
    }
  ]
}
```

### View Message Statistics
```bash
curl http://localhost:5100/admin/wolverine/stats
```

### Check Swagger UI
Open: http://localhost:5100/swagger

## Step 4: Test Transactional Outbox (2 minutes)

### Create an Order with Guaranteed Notifications

```bash
curl -X POST http://localhost:5100/examples/transactional/create-order \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "customerEmail": "customer@example.com",
    "totalAmount": 99.99
  }'
```

Response:
```json
{
  "orderId": "789e4567-e89b-12d3-a456-426614174999",
  "message": "Order created successfully. Notifications are being processed.",
  "estimatedDeliveryMinutes": 5
}
```

**What just happened?**
1. Order data saved to database
2. Two messages stored in outbox table (OrderCreatedEvent, SendEmailCommand)
3. Transaction committed atomically
4. Background agent sends messages within seconds

### Schedule a Future Message

```bash
curl -X POST http://localhost:5100/examples/transactional/schedule-reminder \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "123e4567-e89b-12d3-a456-426614174000",
    "message": "Time for your appointment!",
    "delayMinutes": 2
  }'
```

**What just happened?**
1. Message stored in `wolverine_scheduled_jobs` table
2. Will be delivered automatically in 2 minutes
3. Survives application restarts

## Step 5: Monitor Messages (1 minute)

### View Database Tables

```bash
# Connect to SQL Server
docker exec -it wolverine-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd'
```

Run queries:
```sql
USE WolverineDb;
GO

-- View outbox messages
SELECT id, message_type, destination, attempts 
FROM wolverine.wolverine_outgoing_envelopes;

-- View scheduled jobs
SELECT id, message_type, execution_time 
FROM wolverine.wolverine_scheduled_jobs;

-- View inbox (processed messages)
SELECT id, message_type, status, attempts 
FROM wolverine.wolverine_incoming_envelopes;
```

### Use SQL Scripts

```bash
# Run maintenance script
docker exec -i wolverine-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' \
  -i /scripts/maintenance.sql
```

## 🎉 Success!

You now have:
- ✅ Transactional outbox for guaranteed message delivery
- ✅ Transactional inbox for exactly-once processing
- ✅ Scheduled message delivery
- ✅ Automatic retries with exponential backoff
- ✅ Dead letter queue for failed messages
- ✅ Health monitoring endpoints

## Next Steps

### Explore the API

Visit Swagger UI: http://localhost:5100/swagger

Key endpoints:
- `GET /admin/wolverine/stats` - View message statistics
- `GET /admin/wolverine/health` - Check system health
- `POST /admin/wolverine/release-stuck` - Release stuck messages
- `GET /admin/wolverine/schema` - Get SQL schema script

### Test Failure Scenarios

#### 1. Message Survives App Crash
```bash
# Create order
curl -X POST http://localhost:5100/examples/transactional/create-order \
  -H "Content-Type: application/json" \
  -d '{"customerId": "123e4567-e89b-12d3-a456-426614174000", "customerEmail": "test@example.com", "totalAmount": 50}'

# Immediately kill the app
docker kill wolverine-api

# Check database - message is in outbox
docker exec -it wolverine-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' \
  -Q "USE WolverineDb; SELECT * FROM wolverine.wolverine_outgoing_envelopes;"

# Restart app
docker-compose up -d wolverine-api

# Message will be sent automatically!
```

#### 2. Scheduled Message Survives Restart
```bash
# Schedule reminder for 5 minutes
curl -X POST http://localhost:5100/examples/transactional/schedule-reminder \
  -H "Content-Type: application/json" \
  -d '{"userId": "123e4567-e89b-12d3-a456-426614174000", "message": "Test", "delayMinutes": 5}'

# Restart the app
docker-compose restart wolverine-api

# Check logs - message will still be delivered at scheduled time
docker logs -f wolverine-api
```

#### 3. Exactly-Once Processing
```bash
# Send the same message ID twice
# Wolverine automatically deduplicates using inbox table
```

### Integrate with Your Code

#### Create Handler with Transactional Outbox
```csharp
[WolverineHandler]
[Transactional]
public async Task Handle(
    CreateTodoCommand command,
    ApplicationDbContext db,
    IMessageBus bus)
{
    // Save to database
    var todo = new TodoItem { ... };
    db.Todos.Add(todo);
    
    // Publish event - stored in outbox
    await bus.PublishAsync(new TodoCreatedEvent(todo.Id));
    
    // Both committed atomically
    await db.SaveChangesAsync();
}
```

#### Schedule Future Task
```csharp
// Send reminder 1 hour before due date
var reminderTime = todo.DueDate.AddHours(-1);
await bus.ScheduleAsync(
    new SendReminderCommand(todo.Id),
    reminderTime);
```

#### Configure Retry Policy
```csharp
[WolverineHandler]
[RetryOn(typeof(HttpRequestException), 1000, 2000, 5000)]
[MaximumAttempts(3)]
public async Task Handle(SendEmailCommand command)
{
    await _emailService.SendAsync(command);
}
```

## Common Tasks

### View Logs in Real-Time
```bash
docker-compose logs -f wolverine-api
```

### Clear All Messages (Development Only)
```bash
curl -X POST "http://localhost:5100/admin/wolverine/clear?confirm=true"
```

### Run Maintenance Script
```bash
docker exec -i wolverine-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' \
  -i /app/scripts/maintenance.sql
```

### View Seq Logs
Open: http://localhost:5341

### Backup Database
```bash
docker exec wolverine-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' \
  -Q "BACKUP DATABASE WolverineDb TO DISK='/var/opt/mssql/backup/wolverine.bak'"
```

## Troubleshooting

### Can't Connect to SQL Server
```bash
# Check if container is running
docker ps | grep sqlserver

# Check logs
docker logs wolverine-sqlserver

# Verify health
docker exec wolverine-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' -Q "SELECT 1"
```

### Messages Not Being Processed
1. Check durability agent is enabled:
   ```csharp
   opts.Durability.DurabilityAgentEnabled = true;
   ```

2. Check logs for errors:
   ```bash
   docker logs wolverine-api | grep -i error
   ```

3. Release stuck messages:
   ```bash
   curl -X POST http://localhost:5100/admin/wolverine/release-stuck
   ```

### Schema Not Created
```bash
# Manually run setup script
docker exec -i wolverine-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' \
  -i /app/scripts/setup-database.sql
```

## Performance Tips

1. **Adjust Polling Interval** (in appsettings.json):
   ```json
   {
     "Wolverine": {
       "Durability": {
         "ScheduledJobPollingTime": "00:00:01"
       }
     }
   }
   ```

2. **Use Connection Pooling**:
   ```
   Server=localhost;Min Pool Size=10;Max Pool Size=100;
   ```

3. **Index Optimization**: Run maintenance script regularly

4. **Clean Up Old Messages**: Schedule cleanup job

## Documentation

- 📖 [SQL Server Inbox/Outbox Guide](./docs/SQLSERVER_INBOX_OUTBOX.md)
- 📖 [Wolverine Official Docs](https://wolverine.netlify.app/)
- 📖 [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)

## Support

- GitHub Issues: [Report a problem](https://github.com/JasperFx/wolverine/issues)
- Discord: [Join the community](https://discord.gg/WMxrvegf8H)
- Documentation: [Wolverine Docs](https://wolverine.netlify.app/)

---

**🎊 Congratulations!** You're now using enterprise-grade message reliability with Wolverine and SQL Server!
