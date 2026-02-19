namespace E470.AuditLog.Contracts.Messages;

/// <summary>
/// Message published when an audit log entry is created
/// </summary>
public record AuditLogCreated
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
}
