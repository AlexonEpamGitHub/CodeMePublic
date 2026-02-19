var builder = DistributedApplication.CreateBuilder(args);
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sqlServerMessageBus = builder
    .AddSqlServer("sqlServerMessageBus", port: 5051, password: sqlPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var dbMessageBus = sqlServerMessageBus.AddDatabase("message-bus-mssql");

builder.AddProject<Projects.E470_AuditLog_Produser>("e470-auditlog-produser")
    .WithReference(dbMessageBus)
    .WaitFor(dbMessageBus);

builder.AddProject<Projects.E470_AuditLog_Consumer>("e470-auditlog-consumer")
    .WithReference(dbMessageBus)
    .WaitFor(dbMessageBus);

builder.Build().Run();
