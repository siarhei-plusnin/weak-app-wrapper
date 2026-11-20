using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeakAppWrapper.Processor.Infrastructure.Persistence;

namespace WeakAppWrapper.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddWeakAppPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<ProcessorDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(ProcessorDbContext).Assembly.GetName().Name!);
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null
                    );
                }
            )
        );

        return services;
    }
}
