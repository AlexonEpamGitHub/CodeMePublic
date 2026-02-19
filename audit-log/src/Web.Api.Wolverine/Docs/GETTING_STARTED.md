# Getting Started with Wolverine Web API

This guide will help you get the Wolverine Web API up and running quickly.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (optional, for containerized setup)
- SQL Server (local or containerized)
- Your favorite IDE (Visual Studio 2022, Rider, or VS Code)

## Quick Start (5 minutes)

### Option 1: Docker Compose (Recommended)

The easiest way to get started with all dependencies:

```bash
# Navigate to the project directory
cd audit-log/src/Web.Api.Wolverine

# Start all services (API, SQL Server, Seq)
docker-compose up -d

# View logs
docker-compose logs -f webapi-wolverine

# The API will be available at:
# - HTTP: http://localhost:5100
# - HTTPS: https://localhost:5101
# - Swagger: http://localhost:5100/swagger
# - Seq: http://localhost:5341
```

To stop:
```bash
docker-compose down
```

### Option 2: Local Development

If you prefer running the API locally:

#### 1. Start SQL Server
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

#### 2. (Optional) Start Seq for Logging
```bash
docker run -e ACCEPT_EULA=Y -p 5341:80 -d datalust/seq:latest
```

#### 3. Update Connection String
Edit `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Database": "Server=localhost,1433;Database=AuditLog;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  }
}
```

#### 4. Run the Application
```bash
# From the solution root
cd audit-log

# Run the API
dotnet run --project src/Web.Api.Wolverine/Web.Api.Wolverine.csproj
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger: https://localhost:5001/swagger

## Verify Installation

### 1. Check Health Endpoint
```bash
curl http://localhost:5100/health
```

Expected response:
```json
{
  "status": "Healthy"
}
```

### 2. Check Detailed Health
```bash
curl http://localhost:5100/health/ready
```

Expected response:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database connection is healthy",
      "duration": "00:00:00.0123456"
    },
    {
      "name": "sqlserver",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0234567"
    }
  ],
  "totalDuration": "00:00:00.0358023"
}
```

### 3. Test API Endpoints

#### Create a User
```bash
curl -X POST http://localhost:5100/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

#### Login
```bash
curl -X POST http://localhost:5100/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123"
  }'
```

Save the returned token for authenticated requests.

#### Create a Todo (requires authentication)
```bash
curl -X POST http://localhost:5100/api/todos \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "title": "My First Todo",
    "description": "Testing Wolverine API",
    "priority": 1,
    "dueDate": "2024-12-31T23:59:59Z"
  }'
```

#### Get All Todos
```bash
curl http://localhost:5100/api/todos \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Project Structure

```
Web.Api.Wolverine/
├── Endpoints/              # HTTP endpoint definitions
│   ├── Todos/             # Todo-related endpoints
│   │   ├── TodoEndpoints.cs
│   │   └── AdvancedTodoEndpoints.cs
│   └── Users/             # User-related endpoints
│       └── UserEndpoints.cs
├── Handlers/              # Message handlers
│   └── MessageHandlers.cs
├── Integration/           # Integration with Application layer
│   └── WolverineIntegration.cs
├── Messages/              # Async message definitions
│   └── TodoNotifications.cs
├── Middleware/            # Wolverine policies and middleware
│   └── WolverineExceptionPolicies.cs
├── HealthChecks/          # Health check configuration
│   └── HealthCheckConfiguration.cs
├── Docs/                  # Documentation
│   ├── MIGRATION.md       # Migration guide
│   └── PATTERNS.md        # Common patterns
├── Tests/                 # Example tests
│   └── ExampleTests.cs
├── Program.cs             # Application entry point
├── appsettings.json       # Configuration
├── docker-compose.yml     # Docker composition
└── README.md             # Project overview
```

## Key Concepts

### 1. Message Bus
The `IMessageBus` is the core of Wolverine. Use it to:
- **Invoke** - Execute commands/queries synchronously
- **Publish** - Fire-and-forget async messages
- **Enqueue** - Add to local queue for processing
- **Schedule** - Delay message delivery

Example:
```csharp
// Synchronous - wait for result
var result = await bus.InvokeAsync<Result>(command);

// Asynchronous - fire and forget
await bus.PublishAsync(notification);

// Local queue - durable, retryable
await bus.EnqueueAsync(backgroundTask);

// Scheduled - future delivery
await bus.SchedulePublishAsync(reminder, DateTime.UtcNow.AddHours(1));
```

### 2. HTTP Endpoints
Define endpoints using attributes:

```csharp
public static class MyEndpoint
{
    [WolverineGet("/api/items/{id}")]
    public static async Task<IResult> Handle(Guid id, IMessageBus bus)
    {
        // Handler logic
    }
}
```

Supported attributes:
- `[WolverineGet]`
- `[WolverinePost]`
- `[WolverinePut]`
- `[WolverineDelete]`
- `[WolverinePatch]`

### 3. Message Handlers
Create handlers by convention:

```csharp
public class MyHandler
{
    public async Task Handle(MyMessage message)
    {
        // Process message
    }
}
```

Wolverine automatically discovers and registers handlers.

### 4. Validation
FluentValidation is integrated automatically:

```csharp
public class MyCommandValidator : AbstractValidator<MyCommand>
{
    public MyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
```

Validation runs before handlers - no manual validation needed!

### 5. Error Handling
Configure retry policies in `Program.cs`:

```csharp
opts.Policies.OnException<TimeoutException>()
    .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());
```

## Common Tasks

### Adding a New Endpoint

1. Create endpoint class in `Endpoints/` folder:
```csharp
public static class GetItemsEndpoint
{
    [WolverineGet("/api/items")]
    public static async Task<IResult> Handle(IMessageBus bus)
    {
        var query = new GetItemsQuery();
        var result = await bus.InvokeAsync<Result<List<ItemDto>>>(query);
        return Results.Ok(result.Value);
    }
}
```

2. That's it! Wolverine automatically discovers and registers it.

### Adding Background Processing

1. Define a message:
```csharp
public record SendEmailNotification(string Email, string Subject, string Body);
```

2. Create a handler:
```csharp
public class EmailNotificationHandler
{
    private readonly IEmailService _emailService;

    public EmailNotificationHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(SendEmailNotification notification)
    {
        await _emailService.SendAsync(
            notification.Email,
            notification.Subject,
            notification.Body);
    }
}
```

3. Publish from endpoint:
```csharp
await bus.PublishAsync(new SendEmailNotification(
    email: "user@example.com",
    subject: "Welcome",
    body: "Welcome to our service!"));
```

### Adding Scheduled Tasks

```csharp
// Schedule a reminder for 1 hour from now
await bus.SchedulePublishAsync(
    new SendReminderEmail("user@example.com", "Don't forget!"),
    DateTime.UtcNow.AddHours(1));
```

## Troubleshooting

### Problem: Connection refused to SQL Server
**Solution:** Ensure SQL Server is running:
```bash
docker ps | grep sql
```

If not running, start it:
```bash
docker start wolverine-sqlserver
```

### Problem: Migrations not applied
**Solution:** Run migrations manually:
```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api.Wolverine
```

Or in code (Program.cs already has this in Development mode):
```csharp
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await dbContext.Database.MigrateAsync();
```

### Problem: Endpoints not discovered
**Solution:** Check Wolverine configuration in Program.cs:
```csharp
opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
```

### Problem: 401 Unauthorized on authenticated endpoints
**Solution:** 
1. First register a user
2. Login to get a token
3. Include token in Authorization header: `Bearer YOUR_TOKEN`

### Problem: Can't see logs in Seq
**Solution:** Check Seq is running:
```bash
# Access Seq UI
http://localhost:5341

# Check Seq container
docker logs wolverine-seq
```

## Next Steps

1. **Explore the API** - Use Swagger UI at http://localhost:5100/swagger
2. **Read the docs**:
   - [Migration Guide](Docs/MIGRATION.md) - Migrate from traditional APIs
   - [Patterns Guide](Docs/PATTERNS.md) - Common patterns and best practices
3. **Review Examples** - Check `Endpoints/Todos/AdvancedTodoEndpoints.cs`
4. **Check Logs** - View structured logs in Seq at http://localhost:5341
5. **Run Tests** - See `Tests/ExampleTests.cs` for testing patterns

## Resources

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [GitHub Repository](https://github.com/JasperFx/wolverine)
- [Discord Community](https://discord.gg/WMxrvegf8H)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## Support

For issues or questions:
1. Check the [troubleshooting section](#troubleshooting)
2. Review the [documentation](https://wolverine.netlify.app/)
3. Search existing issues on GitHub
4. Ask in the Discord community

Happy coding! 🚀
