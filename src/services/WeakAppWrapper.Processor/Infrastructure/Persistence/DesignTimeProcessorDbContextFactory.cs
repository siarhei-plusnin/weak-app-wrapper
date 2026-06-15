using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WeakAppWrapper.Processor.Infrastructure.Persistence;

public sealed class DesignTimeProcessorDbContextFactory : IDesignTimeDbContextFactory<ProcessorDbContext>
{
    public ProcessorDbContext CreateDbContext(string[] args)
    {
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<ProcessorDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ProcessorDbContext(optionsBuilder.Options);
    }
}
