using Microsoft.EntityFrameworkCore;
using WeakAppWrapper.Processor.Infrastructure.Persistence;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string is required");

builder.Services.AddDbContext<ProcessorDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsql =>
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null
            )
    )
);

IHost host = builder.Build();

await host.MigrateProcessorDatabaseAsync();
await host.RunAsync();
