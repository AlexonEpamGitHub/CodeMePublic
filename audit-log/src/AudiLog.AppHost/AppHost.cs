IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<SqlServerServerResource> sql = builder.AddSqlServer("sql")
    .WithDataVolume();
IResourceBuilder<SqlServerDatabaseResource> db = sql.AddDatabase("audit-log-db");

builder.AddProject<Projects.Web_Api>("web-api")
    .WithReference(db)
    .WaitFor(db);

await builder.Build().RunAsync();
