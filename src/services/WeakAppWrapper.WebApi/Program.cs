using FluentValidation;
using WeakAppWrapper.Persistence.Infrastructure.Persistence;
using WeakAppWrapper.WebApi.Api;
using WeakAppWrapper.WebApi.Api.Endpoints;
using WeakAppWrapper.WebApi.GraphQL;
using WeakAppWrapper.WebApi.GraphQL.Types;
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
builder
    .Services.AddGraphQLServer()
    .ModifyRequestOptions(options => options.IncludeExceptionDetails = builder.Environment.IsDevelopment())
    .AddQueryType<ReadingQuery>()
    .AddTypeExtension<AggregationQuery>()
    .AddType<ReadingGraphType>()
    .AddType<ReadingPageGraphType>()
    .AddType<ReadingPayloadUnionType>()
    .AddType<AirQualityPayloadType>()
    .AddType<EnergyPayloadType>()
    .AddType<MotionPayloadType>()
    .AddType<UnknownPayloadType>()
    .AddType<AggregationGraphType>()
    .AddType<LatestMetricValueGraphType>()
    .AddType<MetricKeyType>()
    .AddType<ReadingSortFieldType>()
    .AddType<ReadingSortDirectionType>()
    .AddType<TimeBucketType>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
RouteGroupBuilder api = app.MapGroup("/api");
api.MapReadingEndpoints();
api.MapAggregationEndpoints();
api.MapHealthEndpoint();
app.MapGraphQL("/graphql");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/swagger", () => Results.Redirect("/openapi/v1.json")).ExcludeFromDescription();
}

app.Run();
