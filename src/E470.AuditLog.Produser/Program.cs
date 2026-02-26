using E470.AuditLog.Contracts.Messages;
using E470.AuditLog.Produser.Data;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.SqlServer;

namespace E470.AuditLog.Produser;

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
            // Setting up SQL Server-backed message storage for transactional outbox
            // This requires a reference to Wolverine.SqlServer
            opts.PersistMessagesWithSqlServer(connectionString, "wolverine");

            // Set up Entity Framework Core as the support
            // for Wolverine's transactional middleware
            opts.UseEntityFrameworkCoreTransactions();

            // Enrolling all local queues into the
            // durable inbox/outbox processing
            opts.Policies.UseDurableLocalQueues();
            
            // Enable durable outbox pattern for reliable message publishing
            opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

            // Configure message routing - publish to local queue for Consumer
            opts.PublishMessage<AuditLogCreated>()
                .ToLocalQueue("auditlog");
        });

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
