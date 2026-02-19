# Wolverine Patterns & Best Practices

This document provides common patterns and best practices when working with Wolverine in this project.

## Table of Contents
1. [Message Handler Patterns](#message-handler-patterns)
2. [HTTP Endpoint Patterns](#http-endpoint-patterns)
3. [Error Handling](#error-handling)
4. [Validation](#validation)
5. [Testing Patterns](#testing-patterns)
6. [Performance Tips](#performance-tips)

---

## Message Handler Patterns

### 1. Simple Handler
```csharp
public class SimpleHandler
{
    public Task Handle(MyMessage message)
    {
        // Process message
        return Task.CompletedTask;
    }
}
```

### 2. Handler with Dependencies
```csharp
public class HandlerWithDependencies
{
    private readonly ILogger<HandlerWithDependencies> _logger;
    private readonly ApplicationDbContext _dbContext;

    public HandlerWithDependencies(
        ILogger<HandlerWithDependencies> logger,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Handle(MyMessage message)
    {
        _logger.LogInformation("Processing {Message}", message);
        // Use dependencies
    }
}
```

### 3. Handler with Return Value (Cascading Messages)
```csharp
public class CascadingHandler
{
    public AnotherMessage Handle(MyMessage message)
    {
        // Process message
        // Return value will be automatically published by Wolverine
        return new AnotherMessage();
    }
}
```

### 4. Handler with Multiple Cascading Messages
```csharp
public class MultiCascadingHandler
{
    public (Message1, Message2, Message3) Handle(MyMessage message)
    {
        return (new Message1(), new Message2(), new Message3());
    }
}
```

### 5. Static Handler (Better Performance)
```csharp
public static class StaticHandler
{
    public static Task Handle(MyMessage message, ILogger<MyMessage> logger)
    {
        logger.LogInformation("Processing {Message}", message);
        return Task.CompletedTask;
    }
}
```

---

## HTTP Endpoint Patterns

### 1. Simple GET Endpoint
```csharp
public static class GetItemEndpoint
{
    [WolverineGet("/api/items/{id}")]
    public static async Task<IResult> Handle(Guid id, IMessageBus bus)
    {
        var query = new GetItemQuery(id);
        var result = await bus.InvokeAsync<Result<ItemDto>>(query);
        
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.NotFound();
    }
}
```

### 2. POST Endpoint with Validation
```csharp
public static class CreateItemEndpoint
{
    public record CreateItemRequest(string Name, string Description);
    
    [WolverinePost("/api/items")]
    public static async Task<IResult> Handle(
        CreateItemRequest request,
        IMessageBus bus)
    {
        var command = new CreateItemCommand(request.Name, request.Description);
        var result = await bus.InvokeAsync<Result<Guid>>(command);
        
        if (!result.IsSuccess)
            return Results.BadRequest(result.Error);
            
        return Results.Created($"/api/items/{result.Value}", new { id = result.Value });
    }
}
```

### 3. Endpoint with Async Background Processing
```csharp
public static class CreateWithNotificationEndpoint
{
    [WolverinePost("/api/items")]
    public static async Task<IResult> Handle(
        CreateItemRequest request,
        IMessageBus bus)
    {
        // Synchronous creation
        var result = await bus.InvokeAsync<Result<Guid>>(
            new CreateItemCommand(request.Name));
        
        if (!result.IsSuccess)
            return Results.BadRequest(result.Error);
        
        // Async notification (fire and forget)
        await bus.PublishAsync(new ItemCreatedNotification(result.Value));
        
        return Results.Created($"/api/items/{result.Value}", result.Value);
    }
}
```

### 4. Endpoint with Query Parameters
```csharp
public static class SearchItemsEndpoint
{
    [WolverineGet("/api/items")]
    public static async Task<IResult> Handle(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        IMessageBus bus)
    {
        var query = new SearchItemsQuery(search, page, pageSize);
        var result = await bus.InvokeAsync<Result<PagedList<ItemDto>>>(query);
        
        return Results.Ok(result.Value);
    }
}
```

### 5. Endpoint with Authorization
```csharp
public static class SecureEndpoint
{
    [WolverinePost("/api/secure")]
    public static async Task<IResult> Handle(
        SecureCommand command,
        HttpContext context,
        IMessageBus bus)
    {
        // Check authorization
        if (!context.User.Identity?.IsAuthenticated ?? true)
            return Results.Unauthorized();
            
        var result = await bus.InvokeAsync<Result>(command);
        return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
    }
}
```

---

## Error Handling

### 1. Retry Policies
```csharp
// In Program.cs or configuration
opts.Policies.OnException<TimeoutException>()
    .RetryTimes(3)
    .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());
```

### 2. Dead Letter Queue
```csharp
opts.Policies.OnException<Exception>()
    .RetryTimes(3)
    .Then
    .MoveToErrorQueue();
```

### 3. Circuit Breaker Pattern
```csharp
opts.Policies.OnException<HttpRequestException>()
    .RetryTimes(5)
    .CircuitBreaker(3, 30.Seconds()); // Break after 3 failures, reset after 30 seconds
```

### 4. Custom Error Handler
```csharp
public class CustomErrorHandler : IFailureAction
{
    private readonly ILogger<CustomErrorHandler> _logger;

    public CustomErrorHandler(ILogger<CustomErrorHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(EnvelopeContext context, Exception exception)
    {
        _logger.LogError(exception, "Error processing message: {MessageType}", 
            context.Envelope.MessageType);
        
        // Custom error handling logic
        await context.SendFailureAcknowledgementAsync();
    }
}
```

---

## Validation

### 1. FluentValidation Integration
```csharp
// Validator
public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
            
        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

// Enable in Wolverine
opts.UseFluentValidation();
```

### 2. Custom Validation Logic
```csharp
public class ValidatedHandler
{
    public async Task<Result> Handle(MyCommand command, ApplicationDbContext dbContext)
    {
        // Manual validation
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result.Failure(Error.Validation("Name is required"));
            
        // Business rule validation
        var exists = await dbContext.Items.AnyAsync(x => x.Name == command.Name);
        if (exists)
            return Result.Failure(Error.Conflict("Item already exists"));
            
        // Process command
        return Result.Success();
    }
}
```

---

## Testing Patterns

### 1. Unit Test Handler
```csharp
[Fact]
public async Task Handler_ProcessesMessage_Successfully()
{
    // Arrange
    var logger = Mock.Of<ILogger<MyHandler>>();
    var handler = new MyHandler(logger);
    var message = new MyMessage("test");

    // Act
    await handler.Handle(message);

    // Assert
    Mock.Get(logger).Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

### 2. Integration Test with Message Bus
```csharp
[Fact]
public async Task MessageBus_InvokesCommand_ReturnsSuccess()
{
    // Arrange
    var host = await Host.CreateDefaultBuilder()
        .UseWolverine(opts =>
        {
            opts.Services.AddSingleton<IMyService, MyService>();
        })
        .StartAsync();
        
    var bus = host.Services.GetRequiredService<IMessageBus>();
    
    // Act
    var result = await bus.InvokeAsync<Result>(new MyCommand());
    
    // Assert
    Assert.True(result.IsSuccess);
}
```

### 3. Endpoint Test
```csharp
[Fact]
public async Task Endpoint_Returns_Created()
{
    // Arrange
    await using var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    // Act
    var response = await client.PostAsJsonAsync("/api/items", 
        new { name = "Test" });
    
    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

---

## Performance Tips

### 1. Use Static Handlers When Possible
Static handlers have less overhead:
```csharp
public static class FastHandler
{
    public static Task Handle(MyMessage message, ILogger<MyMessage> logger)
    {
        // Process message
        return Task.CompletedTask;
    }
}
```

### 2. Optimize Message Serialization
```csharp
// Use records for messages (smaller memory footprint)
public record MyMessage(string Data, int Value);
```

### 3. Use Local Queues for High Throughput
```csharp
opts.LocalQueue("fast-queue")
    .MaximumParallelMessages(10)
    .Sequential(false); // Allow parallel processing
```

### 4. Batch Processing
```csharp
public class BatchHandler
{
    public async Task Handle(MyMessage[] messages)
    {
        // Process all messages in a batch
        await ProcessBatchAsync(messages);
    }
}
```

### 5. Use Compiled Handlers
```csharp
// In production
opts.OptimizeArtifactWorkflow(TypeLoadMode.Static);
```

### 6. Avoid Blocking Calls
```csharp
// Bad
public Task Handle(MyMessage message)
{
    Thread.Sleep(1000); // Blocks thread
    return Task.CompletedTask;
}

// Good
public async Task Handle(MyMessage message)
{
    await Task.Delay(1000); // Non-blocking
}
```

---

## Additional Resources

- [Wolverine Documentation](https://wolverine.netlify.app/)
- [GitHub Repository](https://github.com/JasperFx/wolverine)
- [Discord Community](https://discord.gg/WMxrvegf8H)
