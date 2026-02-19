using Shared.Messages;
using Wolverine;

namespace Web.Api.Consumer.Handlers;

/// <summary>
/// Handles order-related messages
/// </summary>
public class OrderHandlers
{
    private readonly ILogger<OrderHandlers> _logger;
    private readonly MessageStatisticsService _stats;

    public OrderHandlers(ILogger<OrderHandlers> logger, MessageStatisticsService stats)
    {
        _logger = logger;
        _stats = stats;
    }

    /// <summary>
    /// Handles OrderCreatedEvent messages
    /// Wolverine discovers this automatically through naming convention
    /// </summary>
    public async Task Handle(OrderCreatedEvent message)
    {
        _logger.LogInformation(
            "📦 Processing OrderCreatedEvent: OrderId={OrderId}, Customer={Customer}, Amount={Amount}",
            message.OrderId,
            message.CustomerName,
            message.TotalAmount);

        // Simulate order processing
        await Task.Delay(100);

        // Business logic here:
        // - Save order to database
        // - Update inventory
        // - Create invoice
        // - etc.

        _stats.IncrementProcessed("OrderCreatedEvent");

        _logger.LogInformation(
            "✅ Order processed successfully: OrderId={OrderId}, Items={ItemCount}",
            message.OrderId,
            message.Items.Count);
    }

    /// <summary>
    /// Handles PaymentProcessedEvent messages
    /// </summary>
    public async Task Handle(PaymentProcessedEvent message)
    {
        _logger.LogInformation(
            "💳 Processing PaymentProcessedEvent: PaymentId={PaymentId}, OrderId={OrderId}, Success={Success}",
            message.PaymentId,
            message.OrderId,
            message.IsSuccessful);

        if (message.IsSuccessful)
        {
            _logger.LogInformation(
                "✅ Payment successful: Amount={Amount}, Method={Method}",
                message.Amount,
                message.PaymentMethod);

            // Business logic:
            // - Update order status to Paid
            // - Trigger fulfillment process
            // - Send confirmation email
        }
        else
        {
            _logger.LogWarning(
                "❌ Payment failed: OrderId={OrderId}, Error={Error}",
                message.OrderId,
                message.ErrorMessage);

            // Business logic:
            // - Update order status to PaymentFailed
            // - Notify customer
            // - Schedule retry or cancellation
        }

        await Task.Delay(50);
        _stats.IncrementProcessed("PaymentProcessedEvent");
    }
}

/// <summary>
/// Handles user-related messages
/// </summary>
public class UserHandlers
{
    private readonly ILogger<UserHandlers> _logger;
    private readonly MessageStatisticsService _stats;

    public UserHandlers(ILogger<UserHandlers> logger, MessageStatisticsService stats)
    {
        _logger = logger;
        _stats = stats;
    }

    /// <summary>
    /// Handles UserRegisteredEvent messages
    /// </summary>
    public async Task Handle(UserRegisteredEvent message)
    {
        _logger.LogInformation(
            "👤 Processing UserRegisteredEvent: UserId={UserId}, Email={Email}, Name={Name}",
            message.UserId,
            message.Email,
            message.FullName);

        // Simulate user setup
        await Task.Delay(150);

        // Business logic:
        // - Create user profile
        // - Set up default preferences
        // - Assign default roles
        // - Create welcome dashboard

        _stats.IncrementProcessed("UserRegisteredEvent");

        _logger.LogInformation(
            "✅ User setup completed: UserId={UserId}",
            message.UserId);
    }
}

/// <summary>
/// Handles email-related messages
/// </summary>
public class EmailHandlers
{
    private readonly ILogger<EmailHandlers> _logger;
    private readonly MessageStatisticsService _stats;

    public EmailHandlers(ILogger<EmailHandlers> logger, MessageStatisticsService stats)
    {
        _logger = logger;
        _stats = stats;
    }

    /// <summary>
    /// Handles SendEmailCommand messages
    /// </summary>
    public async Task Handle(SendEmailCommand message)
    {
        _logger.LogInformation(
            "📧 Processing SendEmailCommand: To={To}, Subject={Subject}, Type={Type}",
            message.To,
            message.Subject,
            message.Type);

        // Simulate email sending
        await Task.Delay(200);

        // Business logic:
        // - Validate email address
        // - Apply email template
        // - Send via email service (SendGrid, etc.)
        // - Log email sent

        _stats.IncrementProcessed("SendEmailCommand");

        _logger.LogInformation(
            "✅ Email sent successfully: To={To}, Type={Type}",
            message.To,
            message.Type);
    }
}

/// <summary>
/// Handles audit-related messages
/// </summary>
public class AuditHandlers
{
    private readonly ILogger<AuditHandlers> _logger;
    private readonly MessageStatisticsService _stats;

    public AuditHandlers(ILogger<AuditHandlers> logger, MessageStatisticsService stats)
    {
        _logger = logger;
        _stats = stats;
    }

    /// <summary>
    /// Handles AuditLogEvent messages
    /// </summary>
    public async Task Handle(AuditLogEvent message)
    {
        _logger.LogInformation(
            "📝 Processing AuditLogEvent: Entity={EntityType}/{EntityId}, Action={Action}, User={UserId}",
            message.EntityType,
            message.EntityId,
            message.Action,
            message.UserId ?? "System");

        // Simulate audit log storage
        await Task.Delay(50);

        // Business logic:
        // - Store in audit database
        // - Index for search
        // - Check compliance rules
        // - Alert if suspicious

        _stats.IncrementProcessed("AuditLogEvent");

        _logger.LogInformation(
            "✅ Audit log recorded: Changes={ChangeCount}",
            message.Changes.Count);
    }
}

/// <summary>
/// Service to track message processing statistics
/// </summary>
public class MessageStatisticsService
{
    private readonly Dictionary<string, long> _processedCounts = new();
    private readonly object _lock = new();
    private DateTime _startTime = DateTime.UtcNow;

    public void IncrementProcessed(string messageType)
    {
        lock (_lock)
        {
            if (!_processedCounts.ContainsKey(messageType))
            {
                _processedCounts[messageType] = 0;
            }
            _processedCounts[messageType]++;
        }
    }

    public Dictionary<string, long> GetStatistics()
    {
        lock (_lock)
        {
            return new Dictionary<string, long>(_processedCounts);
        }
    }

    public long GetTotalProcessed()
    {
        lock (_lock)
        {
            return _processedCounts.Values.Sum();
        }
    }

    public TimeSpan GetUptime()
    {
        return DateTime.UtcNow - _startTime;
    }

    public void Reset()
    {
        lock (_lock)
        {
            _processedCounts.Clear();
            _startTime = DateTime.UtcNow;
        }
    }
}
