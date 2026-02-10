using E470.AuditLog.SharedKernel;

namespace E470.AuditLog.Domain.Users;

public sealed record UserRegisteredDomainEvent(Guid UserId) : IDomainEvent;
