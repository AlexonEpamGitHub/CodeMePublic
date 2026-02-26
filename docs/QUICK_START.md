# Quick Start Guide - Wolverine SQL Server Transport

Get up and running with the Wolverine SQL Server transport implementation in 5 minutes!

## Prerequisites

- ✅ .NET 9.0 SDK installed
- ✅ Docker Desktop running (for SQL Server)
- ✅ Visual Studio 2022 or VS Code with C# Dev Kit

## Step 1: Clone and Navigate

```bash
git clone <repository-url>
cd src/E470.AuditLog.Produser.AppHost
```

## Step 2: Set SQL Server Password

```bash
dotnet user-secrets set "Parameters:sql-password" "YourPassword123!"
```

> **Note**: Choose a strong password. This is used for the SQL Server container.

## Step 3: Run the Solution

```bash
dotnet run
```

This will:
- 🐳 Start SQL Server in Docker container
- 🗄️ Create database "message-bus-mssql"
- 🚀 Start Producer API on https://localhost:7001
- 🚀 Start Consumer API on https://localhost:7002
- 📊 Initialize Wolverine message tables

## Step 4: Open Aspire Dashboard

The console will show a URL like:
```
info: Aspire.Dashboard[0]
      Now listening on: https://localhost:17247
```

Open that URL in your browser to access the **Aspire Dashboard**.

## Step 5: Test the Message Flow

### Create an Audit Log (Producer)

Open a new terminal and run:

```bash
curl -X POST https://localhost:7001/api/auditlog \
  -H "Content-Type: application/json" \
  -d '{
    "action": "UserLogin",
    "userId": "john.doe",
    "details": "User logged in from Chrome browser",
    "resource": "/api/auth/login"
  }'
```

**Expected Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Query Processed Logs (Consumer)

Wait 1-2 seconds for processing, then:

```bash
curl https://localhost:7002/api/auditlogquery/processed
```

**Expected Response:**
```json
{
  "page": 1,
  "pageSize": 50,
  "totalCount": 1,
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "action": "UserLogin",
      "userId": "john.doe",
      "details": "User logged in from Chrome browser",
      "timestamp": "2024-01-15T10:30:00Z",
      "ipAddress": "::1",
      "resource": "/api/auth/login",
      "isProcessed": true,
      "processedAt": "2024-01-15T10:30:01Z"
    }
  ]
}
```

### Get Statistics

```bash
curl https://localhost:7002/api/auditlogquery/statistics
```

**Expected Response:**
```json
{
  "totalProcessed": 1,
  "totalUnprocessed": 0,
  "actionStatistics": [
    {
      "action": "UserLogin",
      "count": 1
    }
  ]
}
```

## Step 6: Monitor with Aspire Dashboard

In the Aspire Dashboard (https://localhost:17247):

### View Logs
1. Click on **Logs** tab
2. See real-time logs from Producer and Consumer
3. Filter by service name or log level

### View Traces
1. Click on **Traces** tab
2. See distributed traces for message flow
3. Click on a trace to see the complete journey

### View Metrics
1. Click on **Metrics** tab
2. See performance metrics
3. Monitor CPU, memory, and request rates

## Step 7: Inspect Database

Connect to SQL Server:
- **Host**: localhost
- **Port**: 5051
- **Database**: message-bus-mssql
- **User**: sa
- **Password**: (the one you set in Step 2)

### Check Wolverine Tables

```sql
-- View outbox messages (Producer)
SELECT * FROM wolverine.wolverine_outgoing_messages;

-- View inbox messages (Consumer)
SELECT * FROM wolverine.wolverine_incoming_messages;

-- View failed messages
SELECT * FROM wolverine.wolverine_dead_letters;
```

### Check Application Data

```sql
-- View audit log entries
SELECT 
    Id,
    Action,
    UserId,
    Details,
    Timestamp,
    IsProcessed,
    ProcessedAt
FROM dbo.AuditLogEntries
ORDER BY Timestamp DESC;
```

## 🎯 What Just Happened?

Here's the complete flow:

1. **You** sent POST request to Producer API
2. **Producer** saved audit log to database
3. **Producer** published `AuditLogCreated` message
4. **Wolverine** stored message in outbox table (same transaction)
5. **Wolverine Worker** sent message from outbox to Consumer queue
6. **Consumer** received message and stored in inbox table
7. **Handler** processed message with idempotency check
8. **Handler** saved processed entry to database (same transaction)
9. **Wolverine** marked message as processed and removed from inbox
10. **You** queried the processed result from Consumer API

## 🔍 Explore the APIs

### Producer API

Open https://localhost:7001/swagger (when implemented) or use these endpoints:

```bash
# Create audit log
curl -X POST https://localhost:7001/api/auditlog \
  -H "Content-Type: application/json" \
  -d '{
    "action": "UserLogout",
    "userId": "john.doe",
    "details": "User logged out",
    "resource": "/api/auth/logout"
  }'

# Get specific log
curl https://localhost:7001/api/auditlog/{id}

# List all logs (with pagination)
curl https://localhost:7001/api/auditlog?page=1&pageSize=10
```

### Consumer API

```bash
# Get processed log by ID
curl https://localhost:7002/api/auditlogquery/{id}

# Get all processed logs
curl https://localhost:7002/api/auditlogquery/processed?page=1&pageSize=10

# Get logs by user
curl https://localhost:7002/api/auditlogquery/by-user/john.doe

# Get statistics
curl https://localhost:7002/api/auditlogquery/statistics
```

## 🧪 Test Scenarios

### Test 1: Create Multiple Logs

```bash
for i in {1..5}; do
  curl -X POST https://localhost:7001/api/auditlog \
    -H "Content-Type: application/json" \
    -d "{
      \"action\": \"Action$i\",
      \"userId\": \"user$i\",
      \"details\": \"Test log $i\",
      \"resource\": \"/api/test\"
    }"
  echo ""
done
```

Then check statistics:
```bash
curl https://localhost:7002/api/auditlogquery/statistics
```

### Test 2: Verify Idempotency

1. Stop the Consumer service (Ctrl+C in Aspire)
2. Create a log with Producer
3. Check outbox has the message:
   ```sql
   SELECT * FROM wolverine.wolverine_outgoing_messages;
   ```
4. Start Consumer again
5. Message should be processed once despite any retries

### Test 3: Simulate Failure

1. Stop SQL Server container temporarily:
   ```bash
   docker pause <sql-container-id>
   ```
2. Try to create a log - will fail
3. Start SQL Server:
   ```bash
   docker unpause <sql-container-id>
   ```
4. Try again - will succeed

## 🎨 Visual Studio Experience

If using Visual Studio 2022:

1. Open `src/E470.Wolverine.slnx`
2. Set `E470.AuditLog.Produser.AppHost` as startup project
3. Press F5 to run
4. Aspire Dashboard opens automatically
5. Use Test Explorer for unit tests (when added)

## 📝 Common Commands

### View Container Logs
```bash
docker logs <container-id> -f
```

### Reset Database
```bash
docker stop <sql-container-id>
docker rm <sql-container-id>
# Run the solution again - new container will be created
```

### Check Container Status
```bash
docker ps
```

### Connect to SQL Server via CLI
```bash
docker exec -it <sql-container-id> /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P YourPassword123!
```

## 🐛 Troubleshooting

### Issue: Docker container not starting

**Solution**: Ensure Docker Desktop is running and you have enough resources.

```bash
docker ps -a  # Check container status
docker logs <container-id>  # View logs
```

### Issue: Port already in use

**Solution**: Change ports in `AppHost.cs`:

```csharp
.AddSqlServer("sqlServerMessageBus", port: 5052, password: sqlPassword)
```

### Issue: Connection refused

**Solution**: Wait 30 seconds for SQL Server to fully initialize in container.

### Issue: Messages not processing

**Solution**: Check Consumer logs:
1. Open Aspire Dashboard
2. Go to Logs tab
3. Filter by "e470-auditlog-consumer"
4. Look for errors

### Issue: Database not found

**Solution**: Check connection string:
```bash
# Should be "message-bus-mssql"
curl https://localhost:7002/api/health
```

## 🚀 Next Steps

Now that you're up and running:

1. 📖 Read the **[README.md](../src/README.md)** for detailed documentation
2. 🔧 Explore **[Configuration Examples](./CONFIGURATION_EXAMPLES.md)** for advanced scenarios
3. 📚 Review **[Migration Guide](./MIGRATION_GUIDE.md)** to understand patterns
4. 🎯 Check **[Implementation Summary](./IMPLEMENTATION_SUMMARY.md)** for architecture details

## 💡 Tips

- **Use Aspire Dashboard** - It's your best friend for monitoring
- **Query SQL Tables** - Understand message state at any time
- **Check Dead Letters** - Failed messages go here for investigation
- **Enable Verbose Logging** - Set log level to Debug in appsettings.json
- **Scale Consumers** - Run multiple Consumer instances for load testing

## 🎓 Learning Path

1. ✅ **You are here** - Quick Start
2. 📖 Understand the architecture (README.md)
3. 🔍 Learn about Transactional Inbox/Outbox patterns (MIGRATION_GUIDE.md)
4. ⚙️ Explore advanced configuration (CONFIGURATION_EXAMPLES.md)
5. 🧪 Write integration tests
6. 🚀 Deploy to production

## 📞 Need Help?

- Check the [Wolverine Documentation](https://wolverine.netlify.app/)
- Review [.NET Aspire Docs](https://learn.microsoft.com/en-us/dotnet/aspire/)
- Read about [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)

## ✅ Success Checklist

- [ ] Solution runs without errors
- [ ] Aspire Dashboard accessible
- [ ] Created audit log via Producer API
- [ ] Queried processed log via Consumer API
- [ ] Viewed logs in Aspire Dashboard
- [ ] Inspected SQL Server tables
- [ ] Understand the message flow

Congratulations! You're now ready to build reliable messaging systems with Wolverine! 🎉

---

**Ready to dive deeper?** Check out the [full documentation](../src/README.md)!
