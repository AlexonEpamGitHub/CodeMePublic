using Domain.Todos;
using Wolverine;

namespace Web.Api.Wolverine.Messages;

/// <summary>
/// Message for async processing of todo item creation notification
/// </summary>
public record TodoCreatedNotification(Guid TodoId, string Title);

/// <summary>
/// Message for async processing of todo completion
/// </summary>
public record TodoCompletedNotification(Guid TodoId, string Title);

/// <summary>
/// Handler for async todo created notifications
/// This demonstrates Wolverine's ability to process messages asynchronously in the background
/// </summary>
public class TodoNotificationHandler
{
    private readonly ILogger<TodoNotificationHandler> _logger;

    public TodoNotificationHandler(ILogger<TodoNotificationHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle todo created notifications
    /// This could send emails, push notifications, etc.
    /// </summary>
    public async Task Handle(TodoCreatedNotification notification)
    {
        _logger.LogInformation(
            "Processing todo created notification for TodoId: {TodoId}, Title: {Title}",
            notification.TodoId,
            notification.Title);

        // Simulate async work (e.g., sending email, push notification)
        await Task.Delay(100);

        _logger.LogInformation(
            "Todo created notification processed successfully for TodoId: {TodoId}",
            notification.TodoId);
    }

    /// <summary>
    /// Handle todo completed notifications
    /// This demonstrates separate handler for different message types
    /// </summary>
    public async Task Handle(TodoCompletedNotification notification)
    {
        _logger.LogInformation(
            "Processing todo completed notification for TodoId: {TodoId}, Title: {Title}",
            notification.TodoId,
            notification.Title);

        // Simulate async work
        await Task.Delay(100);

        _logger.LogInformation(
            "Todo completed notification processed successfully for TodoId: {TodoId}",
            notification.TodoId);
    }
}

/// <summary>
/// Example of cascading messages - when one handler completes, it can publish other messages
/// </summary>
public class TodoCreatedHandler
{
    private readonly ILogger<TodoCreatedHandler> _logger;

    public TodoCreatedHandler(ILogger<TodoCreatedHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// This handler returns a cascading message that will be automatically published
    /// Wolverine will handle this message asynchronously
    /// </summary>
    public TodoCreatedNotification Handle(Domain.Todos.TodoItemCreatedDomainEvent domainEvent)
    {
        _logger.LogInformation(
            "TodoItemCreatedDomainEvent received, cascading to notification for TodoId: {TodoId}",
            domainEvent.TodoItemId);

        // Return a message that Wolverine will automatically publish and handle
        return new TodoCreatedNotification(
            domainEvent.TodoItemId,
            "Todo Item"); // In real scenario, you'd fetch the title
    }
}

/// <summary>
/// Handler for scheduled/delayed messages
/// This demonstrates Wolverine's ability to schedule messages for future processing
/// </summary>
public record ScheduledTodoReminder(Guid TodoId, string Title, DateTime DueDate);

public class ScheduledReminderHandler
{
    private readonly ILogger<ScheduledReminderHandler> _logger;

    public ScheduledReminderHandler(ILogger<ScheduledReminderHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ScheduledTodoReminder reminder)
    {
        _logger.LogInformation(
            "Reminder for todo {TodoId}: {Title} - Due Date: {DueDate}",
            reminder.TodoId,
            reminder.Title,
            reminder.DueDate);

        // Send reminder notification
        return Task.CompletedTask;
    }
}
