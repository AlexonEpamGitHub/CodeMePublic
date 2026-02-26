using E470.AuditLog.Contracts.Messages;
using E470.AuditLog.Contracts.Models;
using E470.AuditLog.Produser.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace E470.AuditLog.Produser.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogController : ControllerBase
{
    private readonly IMessageBus _messageBus;
    private readonly AuditLogDbContext _dbContext;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(
        IMessageBus messageBus, 
        AuditLogDbContext dbContext,
        ILogger<AuditLogController> logger)
    {
        _messageBus = messageBus;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Creates an audit log entry and publishes it as a message
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAuditLog([FromBody] CreateAuditLogRequest request)
    {
        var auditLogId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        _logger.LogInformation(
            "Creating audit log entry {Id} for user {UserId} with action {Action}",
            auditLogId,
            request.UserId,
            request.Action);

        // Create audit log entry in database
        var auditLogEntry = new AuditLogEntry
        {
            Id = auditLogId,
            Action = request.Action,
            UserId = request.UserId,
            Details = request.Details,
            Timestamp = timestamp,
            IpAddress = request.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            Resource = request.Resource,
            IsProcessed = false
        };

        _dbContext.AuditLogEntries.Add(auditLogEntry);
        await _dbContext.SaveChangesAsync();

        // Publish message to Wolverine
        var message = new AuditLogCreated
        {
            Id = auditLogId,
            Action = request.Action,
            UserId = request.UserId,
            Details = request.Details,
            Timestamp = timestamp,
            IpAddress = auditLogEntry.IpAddress,
            Resource = request.Resource
        };

        await _messageBus.PublishAsync(message);

        _logger.LogInformation(
            "Published AuditLogCreated message {Id} to message bus",
            auditLogId);

        return CreatedAtAction(
            nameof(GetAuditLog), 
            new { id = auditLogId }, 
            new { id = auditLogId, timestamp });
    }

    /// <summary>
    /// Gets an audit log entry by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuditLog(Guid id)
    {
        var auditLog = await _dbContext.AuditLogEntries.FindAsync(id);
        
        if (auditLog == null)
        {
            return NotFound();
        }

        return Ok(auditLog);
    }

    /// <summary>
    /// Gets all audit log entries
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var auditLogs = await _dbContext.AuditLogEntries
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(auditLogs);
    }
}

public record CreateAuditLogRequest
{
    public string Action { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string Resource { get; init; } = string.Empty;
}
