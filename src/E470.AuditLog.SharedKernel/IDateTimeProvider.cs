namespace E470.AuditLog.SharedKernel;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
