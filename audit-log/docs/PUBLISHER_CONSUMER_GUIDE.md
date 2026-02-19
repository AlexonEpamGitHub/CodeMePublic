# Publisher-Consumer Pattern with Wolverine and SQL Server

Complete guide for building reliable publisher-consumer systems using Wolverine messaging framework.

## 🎯 Overview

This implementation demonstrates a production-ready publisher-consumer architecture with:

- **Publisher API** (Port 5200) - Publishes messages via HTTP endpoints
- **Consumer API** (Port 5300+) - Processes messages from durable queues  
- **SQL Server Transport** - Reliable, transactional message delivery
- **Horizontal Scaling** - Multiple consumer instances for load distribution
- **Seq Logging** - Centralized logging and monitoring

## 🏗️ Architecture

```
HTTP Client → Publisher API → Wolverine → SQL Server Queues → Consumer API(s) → Business Logic
```

### Message Flow

1. **HTTP Request** → Publisher API receives request
2. **Publish** → Message stored in SQL Server (durable outbox)
3. **Transport** → Wolverine delivers to appropriate queue
4. **Consume** → Consumer picks up message (durable inbox)
5. **Process** → Handler executes business logic
6. **Complete** → Message marked as processed

## 🚀 Quick Start (2 Minutes)

### Using Docker Compose

```bash
# Start all services
cd audit-log
docker-compose -f docker-compose.publisher-consumer.yml up -d

# View logs
docker-compose -f docker-compose.publisher-consumer.yml logs -f

# Test the system
curl -X POST http://localhost:5200/api/publisher/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "John Doe",
    "customerEmail": "john@example.com",
    "totalAmount": 99.99,
    "items": [{"productName": "Widget", "quantity": 2, "unitPrice": 49.99}]
  }'

# Check consumer stats
curl http://localhost:5300/api/consumer/stats
```

### Using .NET CLI

```bash
# Terminal 1: SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# Terminal 2: Publisher
cd audit-log/src/Web.Api.Publisher
dotnet restore && dotnet run

# Terminal 3: Consumer
cd audit-log/src/Web.Api.Consumer
dotnet restore && dotnet run
```

## 📊 Access Points

| Service | URL | Description |
|---------|-----|-------------|
| Publisher API | http://localhost:5200 | Swagger UI for publishing |
| Consumer API | http://localhost:5300 | Swagger UI for statistics |
| Seq Logs | http://localhost:5341 | Centralized logging |

## 📨 Message Types

### 1. Order Created Event

```bash
curl -X POST http://localhost:5200/api/publisher/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Jane Smith",
    "customerEmail": "jane@example.com",
    "totalAmount": 150.00,
    "items": [
      {"productName": "Premium Widget", "quantity": 1, "unitPrice": 150.00}
    ]
  }'
```

**Result:** Creates `OrderCreatedEvent` + `SendEmailCommand` (confirmation email)

### 2. User Registered Event

```bash
curl -X POST http://localhost:5200/api/publisher/users \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@example.com",
    "fullName": "New User"
  }'
```

**Result:** Creates `UserRegisteredEvent` + `SendEmailCommand` (welcome email)

### 3. Payment Processed Event

```bash
curl -X POST http://localhost:5200/api/publisher/payments \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "123e4567-e89b-12d3-a456-426614174000",
    "amount": 99.99,
    "paymentMethod": "CreditCard",
    "isSuccessful": true
  }'
```

### 4. Send Email Command

```bash
curl -X POST http://localhost:5200/api/publisher/emails \
  -H "Content-Type: application/json" \
  -d '{
    "to": "customer@example.com",
    "subject": "Test Email",
    "body": "This is a test email",
    "type": 0
  }'
```

### 5. Audit Log Event

```bash
curl -X POST http://localhost:5200/api/publisher/audit \
  -H "Content-Type: application/json" \
  -d '{
    "entityType": "Order",
    "entityId": "12345",
    "action": "Created",
    "userId": "user123",
    "changes": {"status": "Pending"}
  }'
```

## 📈 Monitoring

### Consumer Statistics

```bash
curl http://localhost:5300/api/consumer/stats
```

Response:
```json
{
  "status": "Running",
  "uptime": "00:15:23",
  "totalMessagesProcessed": 156,
  "messagesByType": {
    "OrderCreatedEvent": 50,
    "SendEmailCommand": 50,
    "UserRegisteredEvent": 25,
    "PaymentProcessedEvent": 20,
    "AuditLogEvent": 11
  },
  "averageRate": 0.17,
  "rateUnit": "messages/second"
}
```

### SQL Server Monitoring

```sql
-- Connect: localhost:1433, sa/YourStrong@Passw0rd
USE WolverineMessaging;

-- Outgoing messages
SELECT TOP 10 * FROM wolverine.wolverine_outgoing_envelopes ORDER BY id DESC;

-- Incoming messages  
SELECT TOP 10 * FROM wolverine.wolverine_incoming_envelopes ORDER BY id DESC;

-- Dead letters
SELECT * FROM wolverine.wolverine_dead_letters;
```

### Seq Logging

Open http://localhost:5341 and query:

```
Application = 'Publisher' and @Level = 'Error'
Application = 'Consumer' and @Message like '%OrderCreated%'
```

## 🔄 Horizontal Scaling

Scale consumer instances for higher throughput:

```bash
# Scale to 5 consumer instances
docker-compose -f docker-compose.publisher-consumer.yml up -d --scale consumer=5

# Wolverine automatically distributes messages across all instances
```

Benefits:
- **Load Distribution** - Work spread across instances
- **High Availability** - Failure tolerance
- **Zero Downtime** - Scale without stopping

## 🧪 Testing Scenarios

### 1. Basic Flow Test

```bash
# Publish order
curl -X POST http://localhost:5200/api/publisher/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Test", "customerEmail": "test@test.com", "totalAmount": 100, "items": []}'

# Check stats (should show 2 messages: order + email)
curl http://localhost:5300/api/consumer/stats
```

### 2. Load Test

```bash
# Publish 100 orders
for i in {1..100}; do
  curl -X POST http://localhost:5200/api/publisher/orders \
    -H "Content-Type: application/json" \
    -d "{\"customerName\": \"Customer $i\", \"customerEmail\": \"customer$i@test.com\", \"totalAmount\": 100, \"items\": []}" &
done

# Check processing rate
sleep 5
curl http://localhost:5300/api/consumer/stats
```

### 3. Resilience Test

```bash
# Stop consumer
docker stop wolverine-consumer

# Publish messages (they queue in SQL Server)
curl -X POST http://localhost:5200/api/publisher/orders ...

# Restart consumer (messages automatically processed)
docker start wolverine-consumer
curl http://localhost:5300/api/consumer/stats
```

## 🛠️ Configuration

### Queue Configuration

| Queue | Mode | Parallelism | Message Types |
|-------|------|-------------|---------------|
| orders | Sequential | 1 | OrderCreated, PaymentProcessed, UserRegistered |
| emails | Parallel | 5 | SendEmailCommand |
| audit | Parallel | 10 | AuditLogEvent |

### Customize Queues

Edit `Program.cs` in Consumer:

```csharp
// High throughput
opts.ListenToLocalQueue("emails")
    .UseDurableInbox()
    .MaximumParallelMessages(20); // Increased

// Order preservation
opts.ListenToLocalQueue("orders")
    .UseDurableInbox()
    .Sequential(); // One at a time
```

## 🔧 Troubleshooting

### Messages Not Processing

**Check:**
1. Consumer is running: `docker ps | grep consumer`
2. SQL Server connection: `curl http://localhost:5300/health`
3. Logs: `docker logs wolverine-consumer`
4. Dead letters: `SELECT * FROM wolverine.wolverine_dead_letters`

### Slow Processing

**Solutions:**
1. Increase parallelism in queue configuration
2. Scale horizontally: `docker-compose up --scale consumer=3`
3. Check handler performance in Seq logs
4. Optimize database queries in handlers

### Connection Errors

**Solutions:**
1. Verify SQL Server: `docker ps | grep sqlserver`
2. Test connection: `docker exec wolverine-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT 1"`
3. Check connection string in `appsettings.json`

## 🎓 Key Features

### ✅ Guaranteed Delivery
Messages persist in SQL Server - survive application restarts

### ✅ Exactly-Once Processing
Automatic deduplication via durable inbox - no duplicates

### ✅ Automatic Retries
Configurable retry policies with exponential backoff

### ✅ Transactional Outbox
Messages and business data saved atomically

### ✅ Horizontal Scaling
Add consumer instances without code changes

### ✅ Dead Letter Queue
Failed messages captured for analysis

### ✅ Observability
Structured logging to Seq, built-in health checks

## 📚 Project Structure

```
audit-log/
├── src/
│   ├── Shared.Messages/              # Message contracts
│   │   ├── Messages.cs
│   │   └── Shared.Messages.csproj
│   ├── Web.Api.Publisher/            # Publisher API
│   │   ├── Endpoints/
│   │   │   └── PublisherEndpoints.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   └── Web.Api.Publisher.csproj
│   └── Web.Api.Consumer/             # Consumer API
│       ├── Endpoints/
│       │   └── ConsumerEndpoints.cs
│       ├── Handlers/
│       │   └── OrderHandlers.cs
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Dockerfile
│       └── Web.Api.Consumer.csproj
└── docker-compose.publisher-consumer.yml
```

## 💡 Best Practices

1. **Use Durable Inbox/Outbox** - Always enable for reliability
2. **Monitor Dead Letters** - Check regularly for failures
3. **Implement Idempotency** - Handlers should handle retries safely
4. **Structure Logging** - Use Serilog with Seq for debugging
5. **Test Failures** - Simulate crashes before production
6. **Scale Horizontally** - Add instances instead of increasing resources
7. **Keep Handlers Simple** - Single responsibility per handler
8. **Version Messages** - Plan for schema evolution
9. **Set Up Alerts** - Monitor message processing failures
10. **Document Contracts** - Clear message documentation

## 🔗 Resources

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [SQL Server Transport](https://wolverine.netlify.app/guide/messaging/transports/sql-server.html)
- [Message Handlers](https://wolverine.netlify.app/guide/handlers/)

## 🎊 Summary

You now have a production-ready publisher-consumer system with:

- ✅ Reliable message delivery via SQL Server
- ✅ Automatic retries and error handling
- ✅ Horizontal scaling support
- ✅ Comprehensive monitoring and logging
- ✅ Clean, maintainable code structure

**Start building reliable distributed systems today! 🚀**
