using Application.Todos.Create;
using Wolverine;
using Wolverine.Http;
using Web.Api.Wolverine.Messages;

namespace Web.Api.Wolverine.Endpoints.Todos;

/// <summary>
/// Advanced example showing Wolverine's async messaging capabilities
/// </summary>
public static class CreateTodoWithNotificationEndpoint
{
    public record CreateTodoRequest(string Title, string Description, int Priority, DateTime? DueDate);
    
    [WolverinePost("/api/v2/todos")]
    public static async Task<IResult> Handle(
        CreateTodoRequest request,
        IMessageBus bus,
        ILogger<CreateTodoRequest> logger)
    {
        // Create the todo using existing command
        var command = new CreateTodoCommand(
            request.Title,
            request.Description,
            request.Priority,
            request.DueDate);
        
        var result = await bus.InvokeAsync<SharedKernel.Result<Guid>>(command);
        
        if (!result.IsSuccess)
        {
            return Results.BadRequest(result.Error);
        }

        var todoId = result.Value;
        
        // Publish async notification message - this will be processed in the background
        await bus.PublishAsync(new TodoCreatedNotification(todoId, request.Title));
        
        logger.LogInformation("Todo created with ID: {TodoId}, notification queued", todoId);
        
        // If there's a due date, schedule a reminder
        if (request.DueDate.HasValue && request.DueDate.Value > DateTime.UtcNow)
        {
            var reminderTime = request.DueDate.Value.AddHours(-1); // Remind 1 hour before
            
            if (reminderTime > DateTime.UtcNow)
            {
                // Schedule a message for future delivery
                await bus.SchedulePublishAsync(
                    new ScheduledTodoReminder(todoId, request.Title, request.DueDate.Value),
                    reminderTime);
                
                logger.LogInformation(
                    "Scheduled reminder for TodoId: {TodoId} at {ReminderTime}",
                    todoId,
                    reminderTime);
            }
        }
        
        return Results.Created($"/api/todos/{todoId}", new { id = todoId });
    }
}

/// <summary>
/// Example showing message enqueueing for local processing
/// </summary>
public static class CompleteTodoWithNotificationEndpoint
{
    [WolverinePut("/api/v2/todos/{id}/complete")]
    public static async Task<IResult> Handle(
        Guid id,
        IMessageBus bus,
        ILogger<CompleteTodoWithNotificationEndpoint> logger)
    {
        var command = new Application.Todos.Complete.CompleteTodoCommand(id);
        var result = await bus.InvokeAsync<SharedKernel.Result>(command);
        
        if (!result.IsSuccess)
        {
            return Results.BadRequest(result.Error);
        }

        // Enqueue notification to local queue for async processing
        await bus.EnqueueAsync(new TodoCompletedNotification(id, "Todo Item"));
        
        logger.LogInformation("Todo completed: {TodoId}, notification enqueued", id);
        
        return Results.NoContent();
    }
}

/// <summary>
/// Example showing immediate execution (invoke) vs async execution (publish/enqueue)
/// </summary>
public static class MessagePatternExamples
{
    /// <summary>
    /// Invoke - Execute immediately and wait for result (Request/Reply pattern)
    /// Use for: Commands and queries that need immediate response
    /// </summary>
    public static async Task<SharedKernel.Result> InvokeExample(IMessageBus bus)
    {
        var command = new CreateTodoCommand("Title", "Description", 1, null);
        return await bus.InvokeAsync<SharedKernel.Result<Guid>>(command);
    }
    
    /// <summary>
    /// Publish - Fire and forget, message processed in background
    /// Use for: Events, notifications, non-critical operations
    /// </summary>
    public static async Task PublishExample(IMessageBus bus)
    {
        var notification = new TodoCreatedNotification(Guid.NewGuid(), "Title");
        await bus.PublishAsync(notification);
        // Returns immediately, message processed asynchronously
    }
    
    /// <summary>
    /// Enqueue - Add to local durable queue for processing
    /// Use for: Background jobs, guaranteed delivery within same process
    /// </summary>
    public static async Task EnqueueExample(IMessageBus bus)
    {
        var notification = new TodoCompletedNotification(Guid.NewGuid(), "Title");
        await bus.EnqueueAsync(notification);
        // Message persisted and will be retried if handler fails
    }
    
    /// <summary>
    /// Schedule - Delay message processing until specific time
    /// Use for: Reminders, scheduled tasks, delayed operations
    /// </summary>
    public static async Task ScheduleExample(IMessageBus bus)
    {
        var reminder = new ScheduledTodoReminder(Guid.NewGuid(), "Title", DateTime.UtcNow.AddDays(1));
        await bus.SchedulePublishAsync(reminder, DateTime.UtcNow.AddHours(1));
        // Message will be delivered and processed in 1 hour
    }
}
