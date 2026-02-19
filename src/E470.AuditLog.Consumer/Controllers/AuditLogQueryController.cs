using E470.AuditLog.Consumer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E470.AuditLog.Consumer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogQueryController : ControllerBase
{
    private readonly AuditLogDbContext _dbContext;
    private readonly ILogger<AuditLogQueryController> _logger;

    public AuditLogQueryController(
        AuditLogDbContext dbContext,
        ILogger<AuditLogQueryController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets a processed audit log entry by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProcessedAuditLog(Guid id)
    {
        var auditLog = await _dbContext.AuditLogEntries
            .FirstOrDefaultAsync(a => a.Id == id && a.IsProcessed);
        
        if (auditLog == null)
        {
            return NotFound();
        }

        return Ok(auditLog);
    }

    /// <summary>
    /// Gets all processed audit log entries
    /// </summary>
    [HttpGet("processed")]
    public async Task<IActionResult> GetAllProcessedAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var auditLogs = await _dbContext.AuditLogEntries
            .Where(a => a.IsProcessed)
            .OrderByDescending(a => a.ProcessedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            totalCount = await _dbContext.AuditLogEntries.CountAsync(a => a.IsProcessed),
            items = auditLogs
        });
    }

    /// <summary>
    /// Gets audit logs by user ID
    /// </summary>
    [HttpGet("by-user/{userId}")]
    public async Task<IActionResult> GetAuditLogsByUser(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var auditLogs = await _dbContext.AuditLogEntries
            .Where(a => a.UserId == userId && a.IsProcessed)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            userId,
            page,
            pageSize,
            totalCount = await _dbContext.AuditLogEntries.CountAsync(a => a.UserId == userId && a.IsProcessed),
            items = auditLogs
        });
    }

    /// <summary>
    /// Gets audit log statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var totalProcessed = await _dbContext.AuditLogEntries.CountAsync(a => a.IsProcessed);
        var totalUnprocessed = await _dbContext.AuditLogEntries.CountAsync(a => !a.IsProcessed);
        
        var actionStats = await _dbContext.AuditLogEntries
            .Where(a => a.IsProcessed)
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            totalProcessed,
            totalUnprocessed,
            actionStatistics = actionStats
        });
    }
}
