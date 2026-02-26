using E470.AuditLog.Consumer.Data;
using E470.AuditLog.Contracts.Messages;
using E470.AuditLog.Contracts.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.Attributes;

namespace E470.AuditLog.Consumer.Handlers;

/// <summary>
/// Handler for processing AuditLogCreated messages
/// Uses Wolverine's transactional middleware with Entity Framework Core
/// </summary>
public class AuditLogCreatedHandler
{
    private readonly ILogger<AuditLogCreatedHandler> _logger;

    public AuditLogCreatedHandler(ILogger<AuditLogCreatedHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the AuditLogCreated message and persists it to the database
    /// The [Transactional] attribute ensures this operation is wrapped in a database transaction
    /// </summary>
    [Transactional]
    public async Task Handle(AuditLogCreated message, AuditLogDbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing AuditLogCreated message: {Id}, Action: {Action}, User: {UserId}", 
            message.Id, 
            message.Action, 
            message.UserId);

        // Check if entry already exists (idempotency)
        var existingEntry = await dbContext.AuditLogEntries
            .FirstOrDefaultAsync(e => e.Id == message.Id, cancellationToken);

        if (existingEntry != null)
        {
            _logger.LogWarning("Audit log entry {Id} already exists, skipping processing", message.Id);
            return;
        }

        // Create new audit log entry
        var auditLogEntry = new AuditLogEntry
        {
            Id = message.Id,
            Action = message.Action,
            UserId = message.UserId,
            Details = message.Details,
            Timestamp = message.Timestamp,
            IpAddress = message.IpAddress,
            Resource = message.Resource,
            IsProcessed = true,
            ProcessedAt = DateTime.UtcNow
        };

        dbContext.AuditLogEntries.Add(auditLogEntry);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully processed AuditLogCreated message: {Id}", 
            message.Id);
    }
}
