using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using WeakAppWrapper.Persistence.Domain;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using Xunit;

namespace WeakAppWrapper.WebApi.Tests;

[CollectionDefinition(Name)]
public sealed class PostgresApiTestGroup : ICollectionFixture<PostgresApiFixture>
{
    public const string Name = "PostgreSQL Web API";
}

public sealed class PostgresApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("weakapp_api_tests")
        .WithUsername("weakapp")
        .WithPassword("weakapp")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextFactory<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContextFactory<ApplicationDbContext>(options =>
                    options.UseNpgsql(
                        _postgres.GetConnectionString(),
                        npgsql =>
                        {
                            npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name!);
                            npgsql.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(10),
                                errorCodesToAdd: null
                            );
                        }
                    )
                );
            });
        });

        using IServiceScope scope = Factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!db.Database.GetConnectionString()!.Contains("weakapp_api_tests", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The test host did not use the isolated PostgreSQL test database.");
        }

        await db.Database.MigrateAsync();
        await SeedAsync(db);
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await _postgres.DisposeAsync();
    }

    private static async Task SeedAsync(ApplicationDbContext db)
    {
        DateTimeOffset firstCapture = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

        Reading[] readings =
        [
            CreateReading("air-office-1", "air_quality", "Office", "{\"co2\":400,\"pm25\":10,\"humidity\":40}", firstCapture),
            CreateReading(
                "air-office-2",
                "air_quality",
                "Office",
                "{\"co2\":500,\"pm25\":20,\"humidity\":50}",
                firstCapture.AddMinutes(15)
            ),
            CreateReading(
                "air-garage-1",
                "air_quality",
                "Garage",
                "{\"co2\":600,\"pm25\":30,\"humidity\":60}",
                firstCapture.AddMinutes(30)
            ),
            CreateReading("energy-office-1", "energy", "Office", "{\"energy\":200.5}", firstCapture.AddMinutes(45)),
            CreateReading(
                "motion-office-1",
                "motion",
                "Office",
                "{\"motionDetected\":true}",
                firstCapture.AddHours(1)
            ),
            CreateReading(
                "motion-garage-1",
                "motion",
                "Garage",
                "{\"motionDetected\":false}",
                firstCapture.AddHours(1).AddMinutes(15)
            ),
            CreateReading(
                "rain-roof-1",
                "rainfall",
                "Roof",
                "{\"millimeters\":1.25}",
                firstCapture.AddHours(1).AddMinutes(30)
            ),
        ];

        await db.Readings.AddRangeAsync(readings);
        await db.SaveChangesAsync();
    }

    private static Reading CreateReading(
        string messageId,
        string type,
        string name,
        string payloadJson,
        DateTimeOffset capturedAt
    )
    {
        using JsonDocument payload = JsonDocument.Parse(payloadJson);
        string rawRecord = JsonSerializer.Serialize(new
        {
            type,
            name,
            payload = payload.RootElement,
        });

        return new Reading
        {
            Id = Guid.NewGuid(),
            Source = "weakapp",
            SourceMessageId = messageId,
            ItemIndex = 0,
            Type = type,
            Name = name,
            RawRecord = rawRecord,
            CapturedAt = capturedAt,
            ProcessorReceivedAt = capturedAt.AddSeconds(1),
            CreatedAt = capturedAt.AddSeconds(2),
        };
    }
}
