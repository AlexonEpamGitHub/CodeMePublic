using E470.AuditLog.SharedKernel;

namespace E470.AuditLog.Domain.Todos;

public sealed record TodoItemCompletedDomainEvent(Guid TodoItemId) : IDomainEvent;
