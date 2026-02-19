using Application.Todos.Create;
using Wolverine;
using Wolverine.Attributes;
using SharedKernel;

namespace Web.Api.Wolverine.Handlers;

/// <summary>
/// Example handlers demonstrating transactional inbox/outbox patterns with SQL Server
/// </summary>
public static class TransactionalMessageHandlers
{
    /// <summary>
    /// Creates a todo with guaranteed message delivery using transactional outbox.
    /// The notification will be sent even if the application crashes after saving.
    /// </summary>
    [WolverineHandler]
    public static async Task<Result<Guid>> Handle(
        CreateTodoCommand command,
        IApplicationDbContext dbContext,
        IMessageBus bus,
        CancellationToken ct)
    {
        // Business logic
        var todoItem = new Domain.Todos.TodoItem
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Description = command.Description,
            Priority = command.Priority,
            DueDate = command.DueDate,
            IsCompleted = false,
            CreatedOnUtc = DateTime.UtcNow
        };

        dbContext.Todos.Add(todoItem);
        
        // Publish notification - this will be stored in the outbox table
        // and sent AFTER the transaction commits successfully
        await bus.PublishAsync(new TodoCreatedNotification(todoItem.Id, todoItem.Title), 
            new DeliveryOptions { DeliverWithin = TimeSpan.FromMinutes(5) });

        // Save changes - this commits both the todo AND the outbox message atomically
        await dbContext.SaveChangesAsync(ct);

        return Result<Guid>.Success(todoItem.Id);
    }
}

/// <summary>
/// Notification sent after a todo is created.
/// This will be reliably delivered via the transactional outbox.
/// </summary>
public record TodoCreatedNotification(Guid TodoId, string Title);

/// <summary>
/// Handler for TodoCreatedNotification
/// This demonstrates inbox processing - messages are deduplicated automatically
/// </summary>
public class TodoCreatedNotificationHandler
{
    private readonly ILogger<TodoCreatedNotificationHandler> _logger;

    public TodoCreatedNotificationHandler(ILogger<TodoCreatedNotificationHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// This handler will execute exactly once, even if the message is retried
    /// </summary>
    [WolverineHandler]
    [MaximumAttempts(3)]
    [RetryOn(typeof(HttpRequestException), 1000, 2000, 5000)] // Exponential backoff
    public async Task Handle(TodoCreatedNotification notification)
    {
        _logger.LogInformation("Todo created notification received for {TodoId}: {Title}", 
            notification.TodoId, notification.Title);

        // Example: Send email, push notification, update cache, etc.
        // If this fails, Wolverine will automatically retry with the configured policy
        await SimulateSendEmailAsync(notification);

        _logger.LogInformation("Todo created notification processed successfully for {TodoId}", 
            notification.TodoId);
    }

    private async Task SimulateSendEmailAsync(TodoCreatedNotification notification)
    {
        // Simulate external service call
        await Task.Delay(100);
        // In real scenario: await _emailService.SendAsync(...)
    }
}

/// <summary>
/// Example of a scheduled message that will be delivered at a specific time
/// </summary>
public record TodoReminderCommand(Guid TodoId, string Title, DateTime DueDate);

/// <summary>
/// Handler for scheduled reminders
/// </summary>
public class TodoReminderHandler
{
    private readonly ILogger<TodoReminderHandler> _logger;
    private readonly IMessageBus _bus;

    public TodoReminderHandler(ILogger<TodoReminderHandler> logger, IMessageBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    /// <summary>
    /// Schedules a reminder to be sent before the todo is due
    /// </summary>
    public async Task ScheduleReminder(Guid todoId, string title, DateTime dueDate)
    {
        var reminderTime = dueDate.AddHours(-1); // 1 hour before due

        if (reminderTime > DateTime.UtcNow)
        {
            // This message will be stored in SQL Server and delivered at the specified time
            await _bus.ScheduleAsync(
                new TodoReminderCommand(todoId, title, dueDate),
                reminderTime);

            _logger.LogInformation("Reminder scheduled for todo {TodoId} at {ReminderTime}", 
                todoId, reminderTime);
        }
    }

    /// <summary>
    /// Handles the scheduled reminder
    /// </summary>
    [WolverineHandler]
    public async Task Handle(TodoReminderCommand command)
    {
        _logger.LogInformation("Sending reminder for todo {TodoId}: {Title} due at {DueDate}",
            command.TodoId, command.Title, command.DueDate);

        // Send reminder notification
        await Task.Delay(100); // Simulate sending
    }
}

/// <summary>
/// Example of a saga/workflow using SQL Server persistence
/// </summary>
[Transactional] // This ensures all operations are part of the same transaction
public class OrderProcessingSaga
{
    private readonly ILogger<OrderProcessingSaga> _logger;
    private readonly IMessageBus _bus;

    public OrderProcessingSaga(ILogger<OrderProcessingSaga> logger, IMessageBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    /// <summary>
    /// Step 1: Start the order process
    /// </summary>
    [WolverineHandler]
    public async Task Handle(StartOrderCommand command)
    {
        _logger.LogInformation("Starting order {OrderId}", command.OrderId);

        // Publish multiple messages as part of the saga
        // All will be stored in outbox and sent atomically
        await _bus.PublishAsync(new ValidateInventoryCommand(command.OrderId));
        await _bus.PublishAsync(new ReservePaymentCommand(command.OrderId));

        _logger.LogInformation("Order {OrderId} saga initiated", command.OrderId);
    }

    /// <summary>
    /// Step 2: Handle inventory validation
    /// </summary>
    [WolverineHandler]
    public async Task Handle(InventoryValidatedEvent evt)
    {
        _logger.LogInformation("Inventory validated for order {OrderId}", evt.OrderId);

        // Continue the saga
        await _bus.PublishAsync(new ProcessShippingCommand(evt.OrderId));
    }
}

// Saga message types
public record StartOrderCommand(Guid OrderId);
public record ValidateInventoryCommand(Guid OrderId);
public record ReservePaymentCommand(Guid OrderId);
public record InventoryValidatedEvent(Guid OrderId);
public record ProcessShippingCommand(Guid OrderId);
