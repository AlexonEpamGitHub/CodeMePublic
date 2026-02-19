namespace E470.AuditLog.Contracts.Models;

/// <summary>
/// Domain entity representing an audit log entry
/// </summary>
public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
