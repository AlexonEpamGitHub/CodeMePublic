using Microsoft.Data.SqlClient;
using Wolverine;
using Wolverine.SqlServer;

namespace Web.Api.Wolverine.Infrastructure.Persistence;

/// <summary>
/// Helper class for setting up Wolverine's SQL Server persistence schema
/// </summary>
public static class WolverineSqlServerSetup
{
    /// <summary>
    /// Ensures the Wolverine database schema exists and creates all necessary tables
    /// </summary>
    public static async Task EnsureWolverineTablesExistAsync(this IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            logger.LogInformation("Ensuring Wolverine SQL Server tables exist...");
            
            // Get the Wolverine runtime to access storage
            var runtime = host.Services.GetRequiredService<IWolverineRuntime>();
            
            // Initialize the database schema
            await runtime.Storage.Database.EnsureStorageExistsAsync(typeof(Program));
            
            logger.LogInformation("Wolverine SQL Server tables created successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Wolverine SQL Server tables");
            throw;
        }
    }

    /// <summary>
    /// Validates the SQL Server connection before starting the application
    /// </summary>
    public static async Task ValidateSqlServerConnectionAsync(string connectionString, ILogger logger)
    {
        try
        {
            logger.LogInformation("Validating SQL Server connection...");
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            logger.LogInformation("SQL Server connection validated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to SQL Server. Ensure the database is running.");
            throw;
        }
    }

    /// <summary>
    /// Creates the Wolverine database if it doesn't exist
    /// </summary>
    public static async Task EnsureDatabaseExistsAsync(string connectionString, ILogger logger)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var databaseName = builder.InitialCatalog;
            builder.InitialCatalog = "master";

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $@"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{databaseName}')
                BEGIN
                    CREATE DATABASE [{databaseName}];
                END";

            await command.ExecuteNonQueryAsync();
            
            logger.LogInformation("Database '{DatabaseName}' is ready", databaseName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure database exists");
            throw;
        }
    }

    /// <summary>
    /// Generates SQL script to create Wolverine tables manually (useful for deployment)
    /// </summary>
    public static async Task<string> GenerateSchemaScriptAsync(IHost host)
    {
        var runtime = host.Services.GetRequiredService<IWolverineRuntime>();
        var storage = runtime.Storage;
        
        // This will generate the complete schema creation script
        var script = await storage.Database.CreateDatabaseSchemaAsync();
        
        return script;
    }

    /// <summary>
    /// Clears all pending messages from inbox/outbox (useful for testing/development)
    /// </summary>
    public static async Task ClearAllMessagesAsync(IHost host, ILogger logger)
    {
        logger.LogWarning("Clearing all Wolverine messages from database...");
        
        var runtime = host.Services.GetRequiredService<IWolverineRuntime>();
        await runtime.Storage.ClearAllAsync();
        
        logger.LogInformation("All Wolverine messages cleared");
    }

    /// <summary>
    /// Gets statistics about current messages in the system
    /// </summary>
    public static async Task<MessageStatistics> GetMessageStatisticsAsync(IHost host)
    {
        var runtime = host.Services.GetRequiredService<IWolverineRuntime>();
        var storage = runtime.Storage.Database;

        // Query message counts from Wolverine tables
        return new MessageStatistics
        {
            IncomingCount = await storage.Admin.GetIncomingEnvelopeCountAsync(),
            OutgoingCount = await database.Admin.GetOutgoingEnvelopeCountAsync(),
            ScheduledCount = await storage.Admin.GetScheduledEnvelopeCountAsync()
        };
    }
}

/// <summary>
/// Statistics about messages in the Wolverine system
/// </summary>
public record MessageStatistics
{
    public int IncomingCount { get; init; }
    public int OutgoingCount { get; init; }
    public int ScheduledCount { get; init; }
    public int TotalCount => IncomingCount + OutgoingCount + ScheduledCount;
}
