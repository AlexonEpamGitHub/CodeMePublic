namespace E470.AuditLog.SharedKernel;

public interface IDateTimeProvider
{
    public DateTime UtcNow { get; }
}
