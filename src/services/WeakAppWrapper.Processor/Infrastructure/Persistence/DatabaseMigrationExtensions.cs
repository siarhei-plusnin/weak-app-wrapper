using Microsoft.EntityFrameworkCore;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;

namespace WeakAppWrapper.Processor.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateProcessorDatabaseAsync(this IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
