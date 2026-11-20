using Microsoft.EntityFrameworkCore;

namespace WeakAppWrapper.Processor.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateProcessorDatabaseAsync(this IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();
        ProcessorDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProcessorDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
