using E470.AuditLog.Consumer.Data;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.SqlServer;

namespace E470.AuditLog.Consumer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Get connection string - matches AppHost database configuration
        var connectionString = builder.Configuration.GetConnectionString("message-bus-mssql")!;

        // Register Entity Framework Core DbContext
        builder.Services.AddDbContext<AuditLogDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Configure Wolverine with SQL Server transport
        builder.Host.UseWolverine(opts =>
        {
            // Setting up SQL Server-backed message storage for transactional inbox
            // This requires a reference to Wolverine.SqlServer
            opts.PersistMessagesWithSqlServer(connectionString, "wolverine");

            // Set up Entity Framework Core as the support
            // for Wolverine's transactional middleware
            opts.UseEntityFrameworkCoreTransactions();

            // Enrolling all local queues into the
            // durable inbox/outbox processing
            opts.Policies.UseDurableLocalQueues();
            
            // Enable durable inbox pattern for reliable message consumption
            opts.Policies.UseDurableInboxOnAllListeners();

            // Listen to the local queue for incoming messages
            opts.LocalQueue("auditlog")
                .MaximumParallelMessages(10);

            // Auto-discover message handlers in this assembly
            opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
        });

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
