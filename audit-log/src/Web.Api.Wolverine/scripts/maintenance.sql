-- =====================================================
-- Wolverine SQL Server Maintenance Scripts
-- =====================================================
-- These scripts help with monitoring and maintaining
-- Wolverine's message tables

USE [WolverineDb];
GO

-- =====================================================
-- 1. View Current Message Status
-- =====================================================
PRINT 'Current Message Status:';
PRINT '=====================================================';

-- Outgoing messages summary
SELECT 
    'Outgoing Messages' AS Category,
    COUNT(*) AS Total,
    SUM(CASE WHEN owner_id = 0 THEN 1 ELSE 0 END) AS Pending,
    SUM(CASE WHEN owner_id != 0 THEN 1 ELSE 0 END) AS InProgress,
    SUM(CASE WHEN attempts > 0 THEN 1 ELSE 0 END) AS WithRetries,
    AVG(attempts) AS AvgAttempts,
    MAX(attempts) AS MaxAttempts
FROM wolverine.wolverine_outgoing_envelopes;

-- Incoming messages summary
SELECT 
    'Incoming Messages' AS Category,
    COUNT(*) AS Total,
    SUM(CASE WHEN status = 'Scheduled' THEN 1 ELSE 0 END) AS Scheduled,
    SUM(CASE WHEN status = 'Handled' THEN 1 ELSE 0 END) AS Handled,
    SUM(CASE WHEN status = 'Failed' THEN 1 ELSE 0 END) AS Failed,
    AVG(attempts) AS AvgAttempts,
    MAX(attempts) AS MaxAttempts
FROM wolverine.wolverine_incoming_envelopes;

-- Scheduled jobs summary
SELECT 
    'Scheduled Jobs' AS Category,
    COUNT(*) AS Total,
    SUM(CASE WHEN execution_time <= GETUTCDATE() THEN 1 ELSE 0 END) AS ReadyToExecute,
    SUM(CASE WHEN execution_time > GETUTCDATE() THEN 1 ELSE 0 END) AS Pending,
    MIN(execution_time) AS NextExecution,
    MAX(execution_time) AS LastScheduledFor
FROM wolverine.wolverine_scheduled_jobs;

-- Dead letters summary
SELECT 
    'Dead Letters' AS Category,
    COUNT(*) AS Total,
    MIN(sent_at) AS OldestFailure,
    MAX(sent_at) AS LatestFailure
FROM wolverine.wolverine_dead_letters;

GO

-- =====================================================
-- 2. View Messages by Type
-- =====================================================
PRINT '';
PRINT 'Messages by Type:';
PRINT '=====================================================';

SELECT 
    message_type,
    COUNT(*) AS Count,
    AVG(attempts) AS AvgAttempts
FROM wolverine.wolverine_outgoing_envelopes
GROUP BY message_type
ORDER BY Count DESC;

GO

-- =====================================================
-- 3. Find Stuck Messages (High Retry Count)
-- =====================================================
PRINT '';
PRINT 'Potentially Stuck Messages (3+ attempts):';
PRINT '=====================================================';

SELECT 
    id,
    message_type,
    destination,
    attempts,
    deliver_by,
    owner_id
FROM wolverine.wolverine_outgoing_envelopes
WHERE attempts >= 3
ORDER BY attempts DESC, deliver_by;

GO

-- =====================================================
-- 4. View Recent Dead Letters
-- =====================================================
PRINT '';
PRINT 'Recent Dead Letters (last 24 hours):';
PRINT '=====================================================';

SELECT TOP 100
    id,
    message_type,
    exception_type,
    exception_message,
    sent_at
FROM wolverine.wolverine_dead_letters
WHERE sent_at > DATEADD(HOUR, -24, GETUTCDATE())
ORDER BY sent_at DESC;

GO

-- =====================================================
-- 5. View Upcoming Scheduled Jobs
-- =====================================================
PRINT '';
PRINT 'Next 10 Scheduled Jobs:';
PRINT '=====================================================';

SELECT TOP 10
    id,
    message_type,
    execution_time,
    DATEDIFF(SECOND, GETUTCDATE(), execution_time) AS SecondsUntilExecution,
    attempts
FROM wolverine.wolverine_scheduled_jobs
WHERE execution_time > GETUTCDATE()
ORDER BY execution_time;

GO

-- =====================================================
-- 6. Cleanup Old Messages (Safe - only processed ones)
-- =====================================================
PRINT '';
PRINT 'Cleanup Old Messages (> 7 days):';
PRINT '=====================================================';

-- Preview what will be deleted
SELECT 
    'Would Delete' AS Action,
    COUNT(*) AS Count,
    'Incoming (Handled)' AS FromTable
FROM wolverine.wolverine_incoming_envelopes
WHERE status = 'Handled' 
    AND execution_time < DATEADD(DAY, -7, GETUTCDATE());

-- Uncomment to actually delete:
/*
BEGIN TRANSACTION;

DELETE FROM wolverine.wolverine_incoming_envelopes
WHERE status = 'Handled' 
    AND execution_time < DATEADD(DAY, -7, GETUTCDATE());

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' old handled messages deleted';

COMMIT TRANSACTION;
*/

GO

-- =====================================================
-- 7. Force Release Stuck Messages
-- =====================================================
PRINT '';
PRINT 'Release Stuck Messages (owner_id != 0):';
PRINT '=====================================================';

-- Preview stuck messages
SELECT 
    COUNT(*) AS StuckMessages,
    'Would be released' AS Action
FROM wolverine.wolverine_outgoing_envelopes
WHERE owner_id != 0;

-- Uncomment to release:
/*
BEGIN TRANSACTION;

UPDATE wolverine.wolverine_outgoing_envelopes
SET owner_id = 0
WHERE owner_id != 0;

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' messages released';

COMMIT TRANSACTION;
*/

GO

-- =====================================================
-- 8. Manually Move Message to Dead Letter
-- =====================================================
-- Use this when a message is permanently stuck

/*
BEGIN TRANSACTION;

-- Example: Move a specific message to dead letters
DECLARE @MessageId UNIQUEIDENTIFIER = 'YOUR-MESSAGE-ID-HERE';

INSERT INTO wolverine.wolverine_dead_letters (
    id, 
    execution_time, 
    body, 
    message_type, 
    source,
    exception_type,
    exception_message,
    sent_at
)
SELECT 
    id,
    GETUTCDATE(),
    body,
    message_type,
    destination,
    'Manual',
    'Manually moved to dead letter queue',
    GETUTCDATE()
FROM wolverine.wolverine_outgoing_envelopes
WHERE id = @MessageId;

DELETE FROM wolverine.wolverine_outgoing_envelopes
WHERE id = @MessageId;

COMMIT TRANSACTION;
*/

GO

-- =====================================================
-- 9. Performance Analysis
-- =====================================================
PRINT '';
PRINT 'Table Sizes and Index Usage:';
PRINT '=====================================================';

SELECT 
    t.name AS TableName,
    SUM(ps.row_count) AS RowCount,
    SUM(ps.reserved_page_count * 8) / 1024.0 AS ReservedMB,
    SUM(ps.used_page_count * 8) / 1024.0 AS UsedMB
FROM sys.tables t
INNER JOIN sys.dm_db_partition_stats ps ON t.object_id = ps.object_id
WHERE t.schema_id = SCHEMA_ID('wolverine')
    AND ps.index_id < 2
GROUP BY t.name
ORDER BY ReservedMB DESC;

GO

-- =====================================================
-- 10. Export Statistics (for monitoring)
-- =====================================================
PRINT '';
PRINT 'Summary Statistics (JSON-friendly):';
PRINT '=====================================================';

SELECT 
    GETUTCDATE() AS Timestamp,
    (SELECT COUNT(*) FROM wolverine.wolverine_outgoing_envelopes) AS OutgoingCount,
    (SELECT COUNT(*) FROM wolverine.wolverine_incoming_envelopes WHERE status != 'Handled') AS IncomingCount,
    (SELECT COUNT(*) FROM wolverine.wolverine_scheduled_jobs) AS ScheduledCount,
    (SELECT COUNT(*) FROM wolverine.wolverine_dead_letters) AS DeadLetterCount,
    (SELECT COUNT(*) FROM wolverine.wolverine_outgoing_envelopes WHERE attempts > 3) AS StuckMessageCount,
    (SELECT AVG(attempts) FROM wolverine.wolverine_outgoing_envelopes) AS AvgOutgoingAttempts
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

GO

-- =====================================================
-- 11. Rebuild Indexes (Maintenance)
-- =====================================================
PRINT '';
PRINT 'Rebuild Indexes (for performance):';
PRINT '=====================================================';

-- Check index fragmentation
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent,
    ips.page_count
FROM sys.dm_db_index_physical_stats(
    DB_ID(), 
    NULL, 
    NULL, 
    NULL, 
    'LIMITED'
) ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id 
    AND ips.index_id = i.index_id
WHERE ips.database_id = DB_ID()
    AND OBJECT_SCHEMA_NAME(ips.object_id) = 'wolverine'
    AND ips.avg_fragmentation_in_percent > 10
ORDER BY ips.avg_fragmentation_in_percent DESC;

-- Uncomment to rebuild fragmented indexes:
/*
DECLARE @TableName NVARCHAR(128);
DECLARE @IndexName NVARCHAR(128);
DECLARE @SQL NVARCHAR(MAX);

DECLARE index_cursor CURSOR FOR
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE OBJECT_SCHEMA_NAME(ips.object_id) = 'wolverine'
    AND ips.avg_fragmentation_in_percent > 30
    AND i.name IS NOT NULL;

OPEN index_cursor;
FETCH NEXT FROM index_cursor INTO @TableName, @IndexName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @SQL = 'ALTER INDEX ' + QUOTENAME(@IndexName) + 
               ' ON wolverine.' + QUOTENAME(@TableName) + ' REBUILD;';
    PRINT 'Rebuilding: ' + @SQL;
    EXEC sp_executesql @SQL;
    
    FETCH NEXT FROM index_cursor INTO @TableName, @IndexName;
END

CLOSE index_cursor;
DEALLOCATE index_cursor;
*/

GO

PRINT '';
PRINT '=====================================================';
PRINT 'Maintenance script completed!';
PRINT '=====================================================';
