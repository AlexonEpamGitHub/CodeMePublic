-- =============================================
-- Wolverine SQL Server Transport - Database Initialization
-- This script creates the necessary schema and tables for Wolverine messaging
-- Run this after the database is created
-- =============================================

USE [message-bus-mssql]
GO

-- Create Wolverine schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'wolverine')
BEGIN
    EXEC('CREATE SCHEMA [wolverine]')
    PRINT 'Created schema: wolverine'
END
GO

-- =============================================
-- Wolverine Message Tables
-- These tables are typically auto-created by Wolverine
-- but are included here for reference and manual setup if needed
-- =============================================

-- Outgoing Messages (Outbox Pattern)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[wolverine].[wolverine_outgoing_messages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [wolverine].[wolverine_outgoing_messages](
        [id] [bigint] IDENTITY(1,1) NOT NULL,
        [message_type] [nvarchar](250) NOT NULL,
        [serialized_message] [varbinary](max) NOT NULL,
        [status] [nvarchar](25) NOT NULL,
        [owner_id] [int] NOT NULL,
        [destination] [nvarchar](250) NOT NULL,
        [deliver_by] [datetime2](7) NULL,
        [attempts] [int] NOT NULL DEFAULT 0,
        [body] [varbinary](max) NULL,
        [tenant_id] [nvarchar](100) NULL,
        CONSTRAINT [PK_wolverine_outgoing_messages] PRIMARY KEY CLUSTERED ([id] ASC)
    )
    
    CREATE INDEX [IX_wolverine_outgoing_status] ON [wolverine].[wolverine_outgoing_messages]([status])
    CREATE INDEX [IX_wolverine_outgoing_owner] ON [wolverine].[wolverine_outgoing_messages]([owner_id])
    CREATE INDEX [IX_wolverine_outgoing_destination] ON [wolverine].[wolverine_outgoing_messages]([destination])
    
    PRINT 'Created table: wolverine.wolverine_outgoing_messages'
END
GO

-- Incoming Messages (Inbox Pattern)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[wolverine].[wolverine_incoming_messages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [wolverine].[wolverine_incoming_messages](
        [id] [bigint] IDENTITY(1,1) NOT NULL,
        [message_type] [nvarchar](250) NOT NULL,
        [serialized_message] [varbinary](max) NOT NULL,
        [status] [nvarchar](25) NOT NULL,
        [owner_id] [int] NOT NULL,
        [received_at] [datetime2](7) NOT NULL,
        [attempts] [int] NOT NULL DEFAULT 0,
        [body] [varbinary](max) NULL,
        [tenant_id] [nvarchar](100) NULL,
        CONSTRAINT [PK_wolverine_incoming_messages] PRIMARY KEY CLUSTERED ([id] ASC)
    )
    
    CREATE INDEX [IX_wolverine_incoming_status] ON [wolverine].[wolverine_incoming_messages]([status])
    CREATE INDEX [IX_wolverine_incoming_owner] ON [wolverine].[wolverine_incoming_messages]([owner_id])
    CREATE INDEX [IX_wolverine_incoming_received] ON [wolverine].[wolverine_incoming_messages]([received_at])
    
    PRINT 'Created table: wolverine.wolverine_incoming_messages'
END
GO

-- Dead Letter Queue
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[wolverine].[wolverine_dead_letters]') AND type in (N'U'))
BEGIN
    CREATE TABLE [wolverine].[wolverine_dead_letters](
        [id] [bigint] IDENTITY(1,1) NOT NULL,
        [message_type] [nvarchar](250) NOT NULL,
        [serialized_message] [varbinary](max) NOT NULL,
        [received_at] [datetime2](7) NOT NULL,
        [source] [nvarchar](250) NULL,
        [exception_type] [nvarchar](250) NULL,
        [exception_message] [nvarchar](max) NULL,
        [attempts] [int] NOT NULL DEFAULT 0,
        [body] [varbinary](max) NULL,
        [tenant_id] [nvarchar](100) NULL,
        CONSTRAINT [PK_wolverine_dead_letters] PRIMARY KEY CLUSTERED ([id] ASC)
    )
    
    CREATE INDEX [IX_wolverine_dead_letters_received] ON [wolverine].[wolverine_dead_letters]([received_at])
    CREATE INDEX [IX_wolverine_dead_letters_type] ON [wolverine].[wolverine_dead_letters]([message_type])
    
    PRINT 'Created table: wolverine.wolverine_dead_letters'
END
GO

-- =============================================
-- Application Tables
-- =============================================

-- Create dbo schema tables for application data
-- Producer Database Tables
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogEntries]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLogEntries](
        [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
        [Action] [nvarchar](100) NOT NULL,
        [UserId] [nvarchar](100) NOT NULL,
        [Details] [nvarchar](2000) NULL,
        [Timestamp] [datetime2](7) NOT NULL,
        [IpAddress] [nvarchar](50) NULL,
        [Resource] [nvarchar](500) NULL,
        [IsProcessed] [bit] NOT NULL DEFAULT 0,
        [ProcessedAt] [datetime2](7) NULL
    )
    
    CREATE INDEX [IX_AuditLogEntries_Timestamp] ON [dbo].[AuditLogEntries]([Timestamp])
    CREATE INDEX [IX_AuditLogEntries_UserId] ON [dbo].[AuditLogEntries]([UserId])
    CREATE INDEX [IX_AuditLogEntries_IsProcessed] ON [dbo].[AuditLogEntries]([IsProcessed])
    
    PRINT 'Created table: dbo.AuditLogEntries'
END
GO

-- =============================================
-- Stored Procedures for Monitoring
-- =============================================

-- Get pending outgoing messages count
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[wolverine].[sp_GetPendingOutgoingCount]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [wolverine].[sp_GetPendingOutgoingCount]
GO

CREATE PROCEDURE [wolverine].[sp_GetPendingOutgoingCount]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) as PendingCount,
        destination,
        status
    FROM [wolverine].[wolverine_outgoing_messages]
    WHERE status IN ('Scheduled', 'OutgoingMessages')
    GROUP BY destination, status
END
GO

-- Get pending incoming messages count
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[wolverine].[sp_GetPendingIncomingCount]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [wolverine].[sp_GetPendingIncomingCount]
GO

CREATE PROCEDURE [wolverine].[sp_GetPendingIncomingCount]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) as PendingCount,
        message_type,
        status
    FROM [wolverine].[wolverine_incoming_messages]
    WHERE status IN ('Scheduled', 'Incoming')
    GROUP BY message_type, status
END
GO

-- Get dead letter statistics
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[wolverine].[sp_GetDeadLetterStats]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [wolverine].[sp_GetDeadLetterStats]
GO

CREATE PROCEDURE [wolverine].[sp_GetDeadLetterStats]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) as DeadLetterCount,
        message_type,
        exception_type,
        MAX(received_at) as LastOccurrence
    FROM [wolverine].[wolverine_dead_letters]
    GROUP BY message_type, exception_type
    ORDER BY DeadLetterCount DESC
END
GO

PRINT 'Database initialization completed successfully!'
PRINT 'Wolverine transport is ready to use.'
GO
