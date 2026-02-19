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
        var connectionString = builder.Configuration.GetConnectionString("message-bus-mssql")!;

        // Add DbContext registration
        builder.Services.AddDbContext<AuditLogDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Add services to the container.
        builder.Host.UseWolverine(opts =>
        {
            // Setting up Sql Server-backed message storage
            // This requires a reference to Wolverine.SqlServer
            opts.PersistMessagesWithSqlServer(connectionString, "wolverine");

            // Configure SQL Server transport for transactional inbox/outbox
            opts.UseSqlServerPersistenceAndTransport(connectionString, "wolverine");

            // Set up Entity Framework Core as the support
            // for Wolverine's transactional middleware
            opts.UseEntityFrameworkCoreTransactions();

            // Enrolling all local queues into the
            // durable inbox/outbox processing
            opts.Policies.UseDurableLocalQueues();

            // Add durable inbox and outbox policies
            opts.Policies.UseDurableInboxOnAllListeners();
            opts.Policies.UseDurableOutboxOnAllSendingEndpoints();

            // Configure message publishing to local queues
            opts.PublishAllMessages().ToLocalQueue("audit-log-queue");
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