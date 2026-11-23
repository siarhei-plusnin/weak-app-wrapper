using FluentValidation;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using WeakAppWrapper.WebApi.Api;
using WeakAppWrapper.WebApi.Api.Endpoints;
using WeakAppWrapper.WebApi.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string is required");

builder.Services.AddWeakAppPersistence(connectionString);
builder.Services.AddValidatorsFromAssemblyContaining<ReadingQueryParametersValidator>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
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
