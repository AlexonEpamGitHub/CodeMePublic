using Application;
using Infrastructure;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Web.Api.Wolverine.HealthChecks;
using Web.Api.Wolverine.Integration;
using Web.Api.Wolverine.Middleware;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Wolverine API",
        Version = "v1",
        Description = "WebAPI with Wolverine framework for message-driven architecture"
    });
});

// Add application and infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWolverineIntegration();

// Add custom health checks
builder.Services.AddCustomHealthChecks(builder.Configuration);

// Configure Wolverine
builder.Host.UseWolverine(opts =>
{
    // Enable HTTP endpoints
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
    
    // Use FluentValidation for message validation
    opts.UseFluentValidation();
    
    // Configure SQL Server persistence for transactional inbox and outbox
    opts.PersistMessagesWithSqlServer(builder.Configuration.GetConnectionString("WolverineDatabase"));
    
    // Configure EF Core integration for transactional outbox
    opts.UseEntityFrameworkCoreTransactions();
    
    // Configure durability settings
    opts.Durability.DurabilityAgentEnabled = true;
    opts.Durability.ScheduledJobPollingTime = TimeSpan.FromSeconds(5);
    opts.Durability.ScheduledJobFirstExecution = TimeSpan.FromSeconds(0);
    
    // Configure local queue for async processing
    opts.LocalQueue("default")
        .Sequential();
    
    // Configure durable local queue for important messages
    opts.LocalQueue("important")
        .UseDurableInbox();
    
    // Optimize for local development
    opts.OptimizeArtifactWorkflow(TypeLoadMode.Auto);
    
    // Configure custom exception policies
    opts.ConfigureExceptionPolicies();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Apply migrations in development
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map Wolverine HTTP endpoints
app.MapWolverineEndpoints();

// Add health checks
app.MapCustomHealthChecks();

// Ensure Wolverine SQL Server schema is created
using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<Wolverine.Persistence.Durability.IMessageStore>();
    if (store is Wolverine.SqlServer.SqlServerMessageStore sqlServerStore)
    {
        await sqlServerStore.Admin.MigrateAsync();
    }
}

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }