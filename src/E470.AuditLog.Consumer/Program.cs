using E470.AuditLog.Consumer;
using E470.AuditLog.Consumer.Data;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.SqlServer;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Get connection string for message bus
var messageBusConnectionString = builder.Configuration.GetConnectionString("message-bus-mssql");

// Add DbContext
builder.Services.AddDbContext<AuditLogDbContext>(options =>
    options.UseSqlServer(messageBusConnectionString));

// Configure Wolverine
builder.Host.UseWolverine(opts =>
{
    // Configure SQL Server persistence for Wolverine messaging
    opts.PersistMessagesWithSqlServer(messageBusConnectionString, "wolverine");
    
    // Enable EF Core transaction integration
    opts.UseEntityFrameworkCoreTransactions();
    
    // Configure durable messaging policies
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.UseDurableInbox();
    
    // Configure local queue listening
    opts.ListenToLocalQueue();
});

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