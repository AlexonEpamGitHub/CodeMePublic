namespace Shared.Messages;

/// <summary>
/// Base interface for all messages
/// </summary>
public interface IMessage
{
    Guid MessageId { get; init; }
    DateTime Timestamp { get; init; }
}

/// <summary>
/// Order created event - published when a new order is created
/// </summary>
public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    decimal TotalAmount,
    List<OrderItemDto> Items) : IMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Order item DTO
/// </summary>
public record OrderItemDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// User registered event - published when a new user registers
/// </summary>
public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string FullName,
    DateTime RegisteredAt) : IMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Payment processed event - published when payment is completed
/// </summary>
public record PaymentProcessedEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string PaymentMethod,
    bool IsSuccessful,
    string? ErrorMessage = null) : IMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Email notification command - sent to trigger email sending
/// </summary>
public record SendEmailCommand(
    string To,
    string Subject,
    string Body,
    EmailType Type) : IMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public enum EmailType
{
    OrderConfirmation,
    WelcomeEmail,
    PaymentReceipt,
    General
}

/// <summary>
/// Inventory update command - sent to update product inventory
/// </summary>
public record UpdateInventoryCommand(
    Guid ProductId,
    int QuantityChange,
    string Reason) : IMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Audit log event - published for audit trail
/// </summary>
public record AuditLogEvent(
    string EntityType,
    string EntityId,
    string Action,
    string? UserId,
    Dictionary<string, string> Changes) : IMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
