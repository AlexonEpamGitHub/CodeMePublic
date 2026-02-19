-- =====================================================
-- Wolverine SQL Server Database Setup
-- =====================================================
-- This script sets up the database and schema for Wolverine
-- You can run this manually or let Wolverine auto-create

-- =====================================================
-- 1. Create Database (if needed)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'WolverineDb')
BEGIN
    CREATE DATABASE [WolverineDb];
    PRINT 'Database WolverineDb created successfully';
END
ELSE
BEGIN
    PRINT 'Database WolverineDb already exists';
END
GO

USE [WolverineDb];
GO

-- =====================================================
-- 2. Create Schema
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'wolverine')
BEGIN
    EXEC('CREATE SCHEMA wolverine');
    PRINT 'Schema wolverine created successfully';
END
ELSE
BEGIN
    PRINT 'Schema wolverine already exists';
END
GO

-- =====================================================
-- 3. Create Node Assignments Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'wolverine_node_assignments' AND schema_id = SCHEMA_ID('wolverine'))
BEGIN
    CREATE TABLE wolverine.wolverine_node_assignments (
        id INT PRIMARY KEY,
        node_number INT NOT NULL,
        description NVARCHAR(500)
    );
    PRINT 'Table wolverine_node_assignments created successfully';
END
GO

-- =====================================================
-- 4. Create Outgoing Envelopes Table (Outbox)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'wolverine_outgoing_envelopes' AND schema_id = SCHEMA_ID('wolverine'))
BEGIN
    CREATE TABLE wolverine.wolverine_outgoing_envelopes (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        owner_id INT NOT NULL DEFAULT 0,
        destination NVARCHAR(250) NOT NULL,
        deliver_by DATETIME2 NULL,
        body VARBINARY(MAX) NOT NULL,
        attempts INT NOT NULL DEFAULT 0,
        message_type NVARCHAR(250) NOT NULL,
        CONSTRAINT FK_outgoing_owner FOREIGN KEY (owner_id) 
            REFERENCES wolverine.wolverine_node_assignments(id)
    );

    -- Indexes for performance
    CREATE INDEX IX_wolverine_outgoing_owner ON wolverine.wolverine_outgoing_envelopes(owner_id);
    CREATE INDEX IX_wolverine_outgoing_deliver_by ON wolverine.wolverine_outgoing_envelopes(deliver_by);
    CREATE INDEX IX_wolverine_outgoing_destination ON wolverine.wolverine_outgoing_envelopes(destination);
    
    PRINT 'Table wolverine_outgoing_envelopes created successfully';
END
GO

-- =====================================================
-- 5. Create Incoming Envelopes Table (Inbox)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'wolverine_incoming_envelopes' AND schema_id = SCHEMA_ID('wolverine'))
BEGIN
    CREATE TABLE wolverine.wolverine_incoming_envelopes (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        status NVARCHAR(25) NOT NULL,
        owner_id INT NOT NULL DEFAULT 0,
        execution_time DATETIME2 NULL,
        attempts INT NOT NULL DEFAULT 0,
        body VARBINARY(MAX) NOT NULL,
        message_type NVARCHAR(250) NOT NULL,
        source NVARCHAR(250) NULL,
        CONSTRAINT FK_incoming_owner FOREIGN KEY (owner_id) 
            REFERENCES wolverine.wolverine_node_assignments(id)
    );

    -- Indexes for performance
    CREATE INDEX IX_wolverine_incoming_owner ON wolverine.wolverine_incoming_envelopes(owner_id);
    CREATE INDEX IX_wolverine_incoming_status ON wolverine.wolverine_incoming_envelopes(status);
    CREATE INDEX IX_wolverine_incoming_execution_time ON wolverine.wolverine_incoming_envelopes(execution_time);
    
    PRINT 'Table wolverine_incoming_envelopes created successfully';
END
GO

-- =====================================================
-- 6. Create Scheduled Jobs Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'wolverine_scheduled_jobs' AND schema_id = SCHEMA_ID('wolverine'))
BEGIN
    CREATE TABLE wolverine.wolverine_scheduled_jobs (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        execution_time DATETIME2 NOT NULL,
        attempts INT NOT NULL DEFAULT 0,
        body VARBINARY(MAX) NOT NULL,
        message_type NVARCHAR(250) NOT NULL,
        node_assignment_id INT NULL,
        CONSTRAINT FK_scheduled_node FOREIGN KEY (node_assignment_id) 
            REFERENCES wolverine.wolverine_node_assignments(id)
    );

    -- Index for performance
    CREATE INDEX IX_wolverine_scheduled_execution_time ON wolverine.wolverine_scheduled_jobs(execution_time);
    
    PRINT 'Table wolverine_scheduled_jobs created successfully';
END
GO

-- =====================================================
-- 7. Create Dead Letter Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'wolverine_dead_letters' AND schema_id = SCHEMA_ID('wolverine'))
BEGIN
    CREATE TABLE wolverine.wolverine_dead_letters (
        id UNIQUEIDENTIFIER PRIMARY KEY,
        execution_time DATETIME2 NOT NULL,
        body VARBINARY(MAX) NOT NULL,
        message_type NVARCHAR(250) NOT NULL,
        source NVARCHAR(250) NULL,
        exception_type NVARCHAR(250) NULL,
        exception_message NVARCHAR(MAX) NULL,
        sent_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    -- Index for querying
    CREATE INDEX IX_wolverine_dead_letters_sent_at ON wolverine.wolverine_dead_letters(sent_at);
    CREATE INDEX IX_wolverine_dead_letters_message_type ON wolverine.wolverine_dead_letters(message_type);
    
    PRINT 'Table wolverine_dead_letters created successfully';
END
GO

-- =====================================================
-- 8. Insert Default Node Assignment
-- =====================================================
IF NOT EXISTS (SELECT * FROM wolverine.wolverine_node_assignments WHERE id = 0)
BEGIN
    INSERT INTO wolverine.wolverine_node_assignments (id, node_number, description)
    VALUES (0, 0, 'Unassigned');
    PRINT 'Default node assignment created';
END
GO

-- =====================================================
-- 9. Create Stored Procedures for Common Operations
-- =====================================================

-- Procedure to get pending outgoing messages
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetPendingOutgoingMessages' AND schema_id = SCHEMA_ID('wolverine'))
    DROP PROCEDURE wolverine.sp_GetPendingOutgoingMessages;
GO

CREATE PROCEDURE wolverine.sp_GetPendingOutgoingMessages
AS
BEGIN
    SELECT 
        id,
        message_type,
        destination,
        attempts,
        deliver_by,
        owner_id
    FROM wolverine.wolverine_outgoing_envelopes
    WHERE owner_id = 0 OR deliver_by IS NOT NULL AND deliver_by <= GETUTCDATE()
    ORDER BY deliver_by, attempts;
END
GO

-- Procedure to get scheduled jobs ready to execute
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetReadyScheduledJobs' AND schema_id = SCHEMA_ID('wolverine'))
    DROP PROCEDURE wolverine.sp_GetReadyScheduledJobs;
GO

CREATE PROCEDURE wolverine.sp_GetReadyScheduledJobs
AS
BEGIN
    SELECT 
        id,
        message_type,
        execution_time,
        attempts
    FROM wolverine.wolverine_scheduled_jobs
    WHERE execution_time <= GETUTCDATE()
    ORDER BY execution_time;
END
GO

-- Procedure to clean up old messages
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CleanupOldMessages' AND schema_id = SCHEMA_ID('wolverine'))
    DROP PROCEDURE wolverine.sp_CleanupOldMessages;
GO

CREATE PROCEDURE wolverine.sp_CleanupOldMessages
    @RetentionDays INT = 7
AS
BEGIN
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    
    -- Clean up handled incoming messages
    DELETE FROM wolverine.wolverine_incoming_envelopes
    WHERE status = 'Handled' AND execution_time < @CutoffDate;
    
    DECLARE @IncomingDeleted INT = @@ROWCOUNT;
    
    -- Clean up old dead letters
    DELETE FROM wolverine.wolverine_dead_letters
    WHERE sent_at < @CutoffDate;
    
    DECLARE @DeadLettersDeleted INT = @@ROWCOUNT;
    
    -- Return summary
    SELECT 
        @IncomingDeleted AS IncomingMessagesDeleted,
        @DeadLettersDeleted AS DeadLettersDeleted;
END
GO

-- Procedure to get message statistics
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetMessageStatistics' AND schema_id = SCHEMA_ID('wolverine'))
    DROP PROCEDURE wolverine.sp_GetMessageStatistics;
GO

CREATE PROCEDURE wolverine.sp_GetMessageStatistics
AS
BEGIN
    SELECT 
        'Outgoing' AS QueueType,
        COUNT(*) AS MessageCount,
        SUM(CASE WHEN attempts > 0 THEN 1 ELSE 0 END) AS RetriedCount,
        AVG(attempts) AS AverageAttempts
    FROM wolverine.wolverine_outgoing_envelopes
    
    UNION ALL
    
    SELECT 
        'Incoming' AS QueueType,
        COUNT(*) AS MessageCount,
        SUM(CASE WHEN attempts > 0 THEN 1 ELSE 0 END) AS RetriedCount,
        AVG(attempts) AS AverageAttempts
    FROM wolverine.wolverine_incoming_envelopes
    WHERE status != 'Handled'
    
    UNION ALL
    
    SELECT 
        'Scheduled' AS QueueType,
        COUNT(*) AS MessageCount,
        SUM(CASE WHEN execution_time <= GETUTCDATE() THEN 1 ELSE 0 END) AS ReadyCount,
        0 AS AverageAttempts
    FROM wolverine.wolverine_scheduled_jobs
    
    UNION ALL
    
    SELECT 
        'DeadLetters' AS QueueType,
        COUNT(*) AS MessageCount,
        0 AS RetriedCount,
        0 AS AverageAttempts
    FROM wolverine.wolverine_dead_letters;
END
GO

-- =====================================================
-- 10. Grant Permissions (adjust as needed)
-- =====================================================
-- GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::wolverine TO [YourAppUser];
-- GO

PRINT '========================================';
PRINT 'Wolverine database setup completed!';
PRINT '========================================';
PRINT 'Tables created:';
PRINT '  - wolverine.wolverine_node_assignments';
PRINT '  - wolverine.wolverine_outgoing_envelopes';
PRINT '  - wolverine.wolverine_incoming_envelopes';
PRINT '  - wolverine.wolverine_scheduled_jobs';
PRINT '  - wolverine.wolverine_dead_letters';
PRINT '';
PRINT 'Stored procedures created:';
PRINT '  - wolverine.sp_GetPendingOutgoingMessages';
PRINT '  - wolverine.sp_GetReadyScheduledJobs';
PRINT '  - wolverine.sp_CleanupOldMessages';
PRINT '  - wolverine.sp_GetMessageStatistics';
PRINT '========================================';
GO

-- =====================================================
-- Quick Test Queries
-- =====================================================

-- View all tables
SELECT 
    t.name AS TableName,
    SUM(p.rows) AS RowCount
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id
WHERE t.schema_id = SCHEMA_ID('wolverine')
    AND p.index_id IN (0,1)
GROUP BY t.name
ORDER BY t.name;
GO

-- Get message statistics
EXEC wolverine.sp_GetMessageStatistics;
GO
