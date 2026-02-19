using Microsoft.AspNetCore.Mvc;
using Shared.Messages;
using Wolverine;

namespace Web.Api.Publisher.Endpoints;

public static class PublisherEndpoints
{
    public static RouteGroupBuilder MapPublisherEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/orders", PublishOrderCreated)
            .WithName("PublishOrderCreated")
            .WithSummary("Publish an OrderCreatedEvent")
            .WithDescription("Publishes a new order created event to the message bus");

        group.MapPost("/users", PublishUserRegistered)
            .WithName("PublishUserRegistered")
            .WithSummary("Publish a UserRegisteredEvent")
            .WithDescription("Publishes a new user registered event to the message bus");

        group.MapPost("/payments", PublishPaymentProcessed)
            .WithName("PublishPaymentProcessed")
            .WithSummary("Publish a PaymentProcessedEvent")
            .WithDescription("Publishes a payment processed event to the message bus");

        group.MapPost("/emails", PublishSendEmail)
            .WithName("PublishSendEmail")
            .WithSummary("Publish a SendEmailCommand")
            .WithDescription("Publishes an email command to the message bus");

        group.MapPost("/audit", PublishAuditLog)
            .WithName("PublishAuditLog")
            .WithSummary("Publish an AuditLogEvent")
            .WithDescription("Publishes an audit log event to the message bus");

        group.MapPost("/batch", PublishBatchMessages)
            .WithName("PublishBatchMessages")
            .WithSummary("Publish multiple messages in batch")
            .WithDescription("Publishes a batch of different message types");

        group.MapGet("/stats", GetPublisherStats)
            .WithName("GetPublisherStats")
            .WithSummary("Get publisher statistics")
            .WithDescription("Returns statistics about published messages");

        return group;
    }

    private static async Task<IResult> PublishOrderCreated(
        [FromBody] CreateOrderRequest request,
        IMessageBus bus,
        ILogger<Program> logger)
    {
        try
        {
            var orderEvent = new OrderCreatedEvent(
                OrderId: Guid.NewGuid(),
                CustomerName: request.CustomerName,
                CustomerEmail: request.CustomerEmail,
                TotalAmount: request.TotalAmount,
                Items: request.Items.Select(i => new OrderItemDto(
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice)).ToList()
            );

            await bus.PublishAsync(orderEvent);

            // Also publish email notification
            var emailCommand = new SendEmailCommand(
                To: request.CustomerEmail,
                Subject: $"Order Confirmation #{orderEvent.OrderId}",
                Body: $"Thank you for your order of ${request.TotalAmount}",
                Type: EmailType.OrderConfirmation
            );

            await bus.PublishAsync(emailCommand);

            logger.LogInformation(
                "📤 Published OrderCreatedEvent: OrderId={OrderId}, Amount={Amount}",
                orderEvent.OrderId,
                orderEvent.TotalAmount);

            return Results.Ok(new
            {
                Success = true,
                OrderId = orderEvent.OrderId,
                MessageId = orderEvent.MessageId,
                Timestamp = orderEvent.Timestamp,
                Message = "Order created event published successfully"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to publish OrderCreatedEvent");
            return Results.Problem(
                title: "Failed to publish message",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static async Task<IResult> PublishUserRegistered(
        [FromBody] RegisterUserRequest request,
        IMessageBus bus,
        ILogger<Program> logger)
    {
        try
        {
            var userEvent = new UserRegisteredEvent(
                UserId: Guid.NewGuid(),
                Email: request.Email,
                FullName: request.FullName,
                RegisteredAt: DateTime.UtcNow
            );

            await bus.PublishAsync(userEvent);

            // Send welcome email
            var emailCommand = new SendEmailCommand(
                To: request.Email,
                Subject: "Welcome to our platform!",
                Body: $"Hello {request.FullName}, welcome aboard!",
                Type: EmailType.WelcomeEmail
            );

            await bus.PublishAsync(emailCommand);

            logger.LogInformation(
                "📤 Published UserRegisteredEvent: UserId={UserId}, Email={Email}",
                userEvent.UserId,
                userEvent.Email);

            return Results.Ok(new
            {
                Success = true,
                UserId = userEvent.UserId,
                MessageId = userEvent.MessageId,
                Message = "User registered event published successfully"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to publish UserRegisteredEvent");
            return Results.Problem(
                title: "Failed to publish message",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static async Task<IResult> PublishPaymentProcessed(
        [FromBody] ProcessPaymentRequest request,
        IMessageBus bus,
        ILogger<Program> logger)
    {
        try
        {
            var paymentEvent = new PaymentProcessedEvent(
                PaymentId: Guid.NewGuid(),
                OrderId: request.OrderId,
                Amount: request.Amount,
                PaymentMethod: request.PaymentMethod,
                IsSuccessful: request.IsSuccessful,
                ErrorMessage: request.ErrorMessage
            );

            await bus.PublishAsync(paymentEvent);

            logger.LogInformation(
                "📤 Published PaymentProcessedEvent: PaymentId={PaymentId}, OrderId={OrderId}, Success={Success}",
                paymentEvent.PaymentId,
                paymentEvent.OrderId,
                paymentEvent.IsSuccessful);

            return Results.Ok(new
            {
                Success = true,
                PaymentId = paymentEvent.PaymentId,
                MessageId = paymentEvent.MessageId,
                Message = "Payment processed event published successfully"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to publish PaymentProcessedEvent");
            return Results.Problem(
                title: "Failed to publish message",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static async Task<IResult> PublishSendEmail(
        [FromBody] SendEmailRequest request,
        IMessageBus bus,
        ILogger<Program> logger)
    {
        try
        {
            var emailCommand = new SendEmailCommand(
                To: request.To,
                Subject: request.Subject,
                Body: request.Body,
                Type: request.Type
            );

            await bus.PublishAsync(emailCommand);

            logger.LogInformation(
                "📤 Published SendEmailCommand: To={To}, Subject={Subject}",
                emailCommand.To,
                emailCommand.Subject);

            return Results.Ok(new
            {
                Success = true,
                MessageId = emailCommand.MessageId,
                Message = "Email command published successfully"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to publish SendEmailCommand");
            return Results.Problem(
                title: "Failed to publish message",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static async Task<IResult> PublishAuditLog(
        [FromBody] AuditLogRequest request,
        IMessageBus bus,
        ILogger<Program> logger)
    {
        try
        {
            var auditEvent = new AuditLogEvent(
                EntityType: request.EntityType,
                EntityId: request.EntityId,
                Action: request.Action,
                UserId: request.UserId,
                Changes: request.Changes
            );

            await bus.PublishAsync(auditEvent);

            logger.LogInformation(
                "📤 Published AuditLogEvent: Entity={EntityType}/{EntityId}, Action={Action}",
                auditEvent.EntityType,
                auditEvent.EntityId,
                auditEvent.Action);

            return Results.Ok(new
            {
                Success = true,
                MessageId = auditEvent.MessageId,
                Message = "Audit log event published successfully"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to publish AuditLogEvent");
            return Results.Problem(
                title: "Failed to publish message",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static async Task<IResult> PublishBatchMessages(
        IMessageBus bus,
        ILogger<Program> logger)
    {
        try
        {
            var messages = new List<object>
            {
                new OrderCreatedEvent(
                    Guid.NewGuid(),
                    "Batch Customer",
                    "batch@example.com",
                    150.00m,
                    new List<OrderItemDto>
                    {
                        new("Product A", 2, 75.00m)
                    }),
                new UserRegisteredEvent(
                    Guid.NewGuid(),
                    "batch@example.com",
                    "Batch User",
                    DateTime.UtcNow),
                new SendEmailCommand(
                    "batch@example.com",
                    "Batch Test",
                    "This is a batch message",
                    EmailType.General)
            };

            foreach (var message in messages)
            {
                await bus.PublishAsync(message);
            }

            logger.LogInformation("📤 Published {Count} messages in batch", messages.Count);

            return Results.Ok(new
            {
                Success = true,
                MessageCount = messages.Count,
                Message = "Batch messages published successfully"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to publish batch messages");
            return Results.Problem(
                title: "Failed to publish messages",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static IResult GetPublisherStats(ILogger<Program> logger)
    {
        // In a real application, you would track these metrics
        return Results.Ok(new
        {
            Status = "Running",
            Uptime = TimeSpan.FromMinutes(15).ToString(),
            MessageTypes = new[]
            {
                "OrderCreatedEvent",
                "UserRegisteredEvent",
                "PaymentProcessedEvent",
                "SendEmailCommand",
                "AuditLogEvent"
            },
            Queues = new[]
            {
                new { Name = "orders", Status = "Active" },
                new { Name = "emails", Status = "Active" },
                new { Name = "audit", Status = "Active" }
            }
        });
    }
}

// Request DTOs
public record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    List<OrderItemRequest> Items);

public record OrderItemRequest(
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record RegisterUserRequest(
    string Email,
    string FullName);

public record ProcessPaymentRequest(
    Guid OrderId,
    decimal Amount,
    string PaymentMethod,
    bool IsSuccessful,
    string? ErrorMessage = null);

public record SendEmailRequest(
    string To,
    string Subject,
    string Body,
    EmailType Type);

public record AuditLogRequest(
    string EntityType,
    string EntityId,
    string Action,
    string? UserId,
    Dictionary<string, string> Changes);
