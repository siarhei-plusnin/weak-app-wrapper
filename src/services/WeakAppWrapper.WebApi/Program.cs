using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using WeakAppWrapper.WebApi.Api.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string is required");

builder.Services.AddWeakAppPersistence(connectionString);

WebApplication app = builder.Build();

app.UseHttpsRedirection();
RouteGroupBuilder api = app.MapGroup("/api");
api.MapReadingEndpoints();
api.MapAggregationEndpoints();
api.MapHealthEndpoint();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/swagger", () => Results.Redirect("/openapi/v1.json")).ExcludeFromDescription();
}

app.Run();
