using WeakAppWrapper.Persistence.Infrastructure.Persistence;

namespace WeakAppWrapper.WebApi.Api.Endpoints;

public static class HealthEndpoint
{
    public static RouteGroupBuilder MapHealthEndpoint(this RouteGroupBuilder api)
    {
        api.MapGet(
                "/health",
                async (ApplicationDbContext db, CancellationToken cancellationToken) =>
                {
                    bool databaseConnected = await db.Database.CanConnectAsync(cancellationToken);
                    return databaseConnected
                        ? Results.Ok(new HealthResponse("healthy", "connected"))
                        : Results.Json(
                            new HealthResponse("unhealthy", "unavailable"),
                            statusCode: StatusCodes.Status503ServiceUnavailable
                        );
                }
            )
            .WithName("GetApiHealth")
            .WithSummary("Check API and PostgreSQL connectivity.");

        return api;
    }

    private sealed record HealthResponse(string Status, string Database);
}
