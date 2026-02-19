# Web.Api.Wolverine

This is a modern ASP.NET Core Web API project built with **Wolverine** framework for message-driven architecture.

## What is Wolverine?

[Wolverine](https://wolverine.netlify.app/) is a next-generation .NET framework for building message-driven applications. It provides:

- **HTTP Endpoints** - Clean, attribute-based routing for Web APIs
- **Mediator Pattern** - In-process messaging with command/query separation
- **Message Processing** - Async message queues with retry policies
- **Transactional Outbox** - Reliable message delivery with EF Core integration
- **Validation** - Built-in FluentValidation support
- **Error Handling** - Sophisticated retry and error policies

## Key Features

### 1. Message-Driven Architecture
All business logic is executed through commands and queries, providing clean separation of concerns:

```csharp
[WolverinePost("/api/todos")]
public static async Task<IResult> Handle(CreateTodoCommand command, IMessageBus bus)
{
    var result = await bus.InvokeAsync<Result>(command);
    return result.IsSuccess ? Results.Created() : Results.BadRequest();
}
```

### 2. Automatic Handler Discovery
Wolverine automatically discovers and registers message handlers based on conventions. Your existing Application layer handlers work seamlessly.

### 3. Built-in Validation
FluentValidation is integrated automatically - validation happens before message handlers are invoked:

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
});
```

### 4. Transactional Outbox Pattern
Messages are persisted transactionally with EF Core for reliable delivery:

```csharp
opts.UseEntityFrameworkCoreTransactions();
```

### 5. Error Handling & Retry Policies
Configure sophisticated error handling and retry logic:

```csharp
opts.Policies.OnException<TimeoutException>()
    .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());
```

## Project Structure

```
Web.Api.Wolverine/
├── Endpoints/
│   ├── Todos/           # Todo API endpoints
│   └── Users/           # User API endpoints
├── Handlers/            # Wolverine message handlers
├── Middleware/          # Exception policies and middleware
├── Properties/          # Launch settings
├── Program.cs           # Application startup
├── appsettings.json     # Configuration
└── Dockerfile          # Container configuration
```

## Dependencies

The project uses the following Wolverine packages:

- **WolverineFx** (4.4.0) - Core framework
- **WolverineFx.Http** (4.4.0) - HTTP endpoint support
- **Wolverine.FluentValidation** (4.4.0) - Validation integration
- **WolverineFx.EntityFrameworkCore** (4.4.0) - EF Core integration

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- SQL Server (local or containerized)
- Visual Studio 2022 or Rider

### Running the Application

1. **Update Connection String** in `appsettings.json`:
```json
"ConnectionStrings": {
  "Database": "Server=localhost;Database=AuditLog;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
}
```

2. **Run the application**:
```bash
dotnet run --project src/Web.Api.Wolverine
```

3. **Access Swagger UI**:
Navigate to `https://localhost:5001/swagger`

### Running with Docker

```bash
docker build -t wolverine-api -f src/Web.Api.Wolverine/Dockerfile .
docker run -p 5000:8080 wolverine-api
```

## API Endpoints

### Todos
- `POST /api/todos` - Create a new todo
- `GET /api/todos` - Get all todos (optional filter: ?isCompleted=true)
- `GET /api/todos/{id}` - Get todo by ID
- `PUT /api/todos/{id}/complete` - Mark todo as complete
- `DELETE /api/todos/{id}` - Delete todo

### Users
- `POST /api/users/register` - Register new user
- `POST /api/users/login` - Login and get JWT token
- `GET /api/users/{id}` - Get user by ID

## Configuration

### Wolverine Options

Configure Wolverine in `Program.cs`:

```csharp
builder.Host.UseWolverine(opts =>
{
    // Enable HTTP endpoints
    opts.Discovery.IncludeType<Program>();
    
    // Use FluentValidation
    opts.UseFluentValidation();
    
    // Configure local queues
    opts.LocalQueue("default").Sequential();
    
    // EF Core transactions
    opts.UseEntityFrameworkCoreTransactions();
});
```

### Logging

The project uses Serilog with multiple sinks:
- **Console** - For development
- **File** - Rolling daily logs
- **Seq** - Structured logging (http://localhost:5341)

## Benefits Over Traditional Minimal API

1. **Convention-Based** - Less boilerplate code
2. **Built-in Mediator** - No need for MediatR
3. **Message Processing** - Built-in async queue support
4. **Transactional Messaging** - Reliable message delivery out of the box
5. **Better Testability** - Easy to test handlers in isolation
6. **Performance** - Optimized for high throughput

## Migration from Web.Api

This project demonstrates how to migrate from traditional endpoint-based APIs to Wolverine's message-driven approach while reusing your existing Application layer (commands, queries, handlers).

Key differences:
- Endpoints use `[WolverinePost]`, `[WolverineGet]`, etc. instead of `MapPost()`, `MapGet()`
- Business logic invocation through `IMessageBus` instead of direct handler calls
- Automatic validation and error handling

## Additional Resources

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [Wolverine GitHub](https://github.com/JasperFx/wolverine)
- [Clean Architecture with Wolverine](https://wolverine.netlify.app/guide/messaging/introduction.html)

## License

This project follows the same license as the parent solution.
