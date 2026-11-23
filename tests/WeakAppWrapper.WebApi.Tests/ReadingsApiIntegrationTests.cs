using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace WeakAppWrapper.WebApi.Tests;

[Collection(PostgresApiTestGroup.Name)]
public sealed class ReadingsApiIntegrationTests(PostgresApiFixture fixture)
{
    [Fact]
    public async Task ReadingsEndpointFiltersSortsAndPaginatesWithTotalCount()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/readings?page=1&pageSize=2&sortBy=name&sortDirection=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;

        Assert.Equal(7, root.GetProperty("totalCount").GetInt64());
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(2, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, root.GetProperty("items").GetArrayLength());
        Assert.Equal("Garage", root.GetProperty("items")[0].GetProperty("name").GetString());
        Assert.Equal("Garage", root.GetProperty("items")[0].GetProperty("locationName").GetString());
        Assert.False(root.GetProperty("items")[0].TryGetProperty("rawRecord", out _));
    }

    [Fact]
    public async Task NumericRangeFilterRequiresAndUsesMetricKey()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(
            "/api/readings?metricKey=co2&minValue=450&maxValue=550"
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement items = document.RootElement.GetProperty("items");
        Assert.Single(items.EnumerateArray());
        Assert.Equal("Office", items[0].GetProperty("name").GetString());
        Assert.Equal(500, items[0].GetProperty("payload").GetProperty("co2").GetDouble());

        using HttpResponseMessage missingMetricResponse = await client.GetAsync("/api/readings?minValue=450");
        Assert.Equal(HttpStatusCode.BadRequest, missingMetricResponse.StatusCode);

        using HttpResponseMessage booleanRangeResponse = await client.GetAsync(
            "/api/readings?metricKey=motionDetected&minValue=0"
        );
        Assert.Equal(HttpStatusCode.BadRequest, booleanRangeResponse.StatusCode);
    }

    [Fact]
    public async Task RequestValidationReturnsFieldSpecificErrorsForTheSelectedEndpoint()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        using HttpResponseMessage rangeResponse = await client.GetAsync(
            "/api/readings?from=2026-11-20T10%3A30%3A00Z&to=2026-11-20T10%3A00%3A00Z"
        );

        Assert.Equal(HttpStatusCode.BadRequest, rangeResponse.StatusCode);
        using JsonDocument rangeDocument = JsonDocument.Parse(await rangeResponse.Content.ReadAsStringAsync());
        JsonElement rangeErrors = rangeDocument.RootElement.GetProperty("errors");
        Assert.Contains("to", rangeErrors.EnumerateObject().Select(error => error.Name));

        using HttpResponseMessage pageResponse = await client.GetAsync(
            "/api/readings?page=2147483647&pageSize=100&sortBy=invalid&sortDirection=up"
        );

        Assert.Equal(HttpStatusCode.BadRequest, pageResponse.StatusCode);
        using JsonDocument pageDocument = JsonDocument.Parse(await pageResponse.Content.ReadAsStringAsync());
        JsonElement pageErrors = pageDocument.RootElement.GetProperty("errors");
        Assert.Contains("page", pageErrors.EnumerateObject().Select(error => error.Name));
        Assert.Contains("sortBy", pageErrors.EnumerateObject().Select(error => error.Name));
        Assert.Contains("sortDirection", pageErrors.EnumerateObject().Select(error => error.Name));

        using HttpResponseMessage bucketResponse = await client.GetAsync("/api/aggregations/by-time?bucket=week&page=0");

        Assert.Equal(HttpStatusCode.BadRequest, bucketResponse.StatusCode);
        using JsonDocument bucketDocument = JsonDocument.Parse(await bucketResponse.Content.ReadAsStringAsync());
        JsonElement bucketErrors = bucketDocument.RootElement.GetProperty("errors");
        Assert.Contains("bucket", bucketErrors.EnumerateObject().Select(error => error.Name));
        Assert.DoesNotContain("page", bucketErrors.EnumerateObject().Select(error => error.Name));
    }

    [Fact]
    public async Task LatestEndpointReturnsLatestReadingPerSourceLocationAndType()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/readings/latest?metricKey=co2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement items = document.RootElement;

        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal("Garage", items[0].GetProperty("name").GetString());
        Assert.Equal("Office", items[1].GetProperty("name").GetString());
        Assert.Equal(500, items[1].GetProperty("payload").GetProperty("co2").GetDouble());

        using HttpResponseMessage boundedResponse = await client.GetAsync(
            "/api/readings/latest?metricKey=co2&from=2026-11-20T10%3A15%3A00Z&to=2026-11-20T10%3A30%3A00Z"
        );
        Assert.Equal(HttpStatusCode.OK, boundedResponse.StatusCode);
        using JsonDocument boundedDocument = JsonDocument.Parse(await boundedResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, boundedDocument.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task LocationAndTimeAggregationsKeepNumericAndBooleanStatisticsSeparate()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        using HttpResponseMessage locationResponse = await client.GetAsync(
            "/api/aggregations/by-location?metricKey=co2"
        );

        string locationBody = await locationResponse.Content.ReadAsStringAsync();
        Assert.True(locationResponse.StatusCode == HttpStatusCode.OK, locationBody);
        using JsonDocument locationDocument = JsonDocument.Parse(locationBody);
        JsonElement office = locationDocument
            .RootElement.EnumerateArray()
            .Single(result => result.GetProperty("name").GetString() == "Office");
        Assert.True(office.GetProperty("count").GetInt64() == 2, locationDocument.RootElement.GetRawText());
        Assert.Equal(450, office.GetProperty("average").GetDouble());
        Assert.Equal(JsonValueKind.Null, office.GetProperty("trueCount").ValueKind);

        using HttpResponseMessage countOnlyResponse = await client.GetAsync("/api/aggregations/by-location");
        Assert.Equal(HttpStatusCode.OK, countOnlyResponse.StatusCode);
        using JsonDocument countOnlyDocument = JsonDocument.Parse(await countOnlyResponse.Content.ReadAsStringAsync());
        JsonElement officeCount = countOnlyDocument
            .RootElement
            .EnumerateArray()
            .Single(result => result.GetProperty("name").GetString() == "Office");
        Assert.Equal(4, officeCount.GetProperty("count").GetInt64());
        Assert.Equal(JsonValueKind.Null, officeCount.GetProperty("average").ValueKind);

        using HttpResponseMessage timeResponse = await client.GetAsync(
            "/api/aggregations/by-time?metricKey=co2&bucket=hour"
        );
        Assert.Equal(HttpStatusCode.OK, timeResponse.StatusCode);
        using JsonDocument timeDocument = JsonDocument.Parse(await timeResponse.Content.ReadAsStringAsync());
        JsonElement hour = Assert.Single(timeDocument.RootElement.EnumerateArray());
        Assert.Equal(3, hour.GetProperty("count").GetInt64());
        Assert.Equal(500, hour.GetProperty("average").GetDouble());

        using HttpResponseMessage booleanResponse = await client.GetAsync(
            "/api/aggregations/by-type?metricKey=motionDetected"
        );
        Assert.Equal(HttpStatusCode.OK, booleanResponse.StatusCode);
        using JsonDocument booleanDocument = JsonDocument.Parse(await booleanResponse.Content.ReadAsStringAsync());
        JsonElement motion = Assert.Single(booleanDocument.RootElement.EnumerateArray());
        Assert.Equal(1, motion.GetProperty("trueCount").GetInt64());
        Assert.Equal(1, motion.GetProperty("falseCount").GetInt64());
        Assert.Equal(0.5, motion.GetProperty("trueRate").GetDouble());
        Assert.Equal(JsonValueKind.Null, motion.GetProperty("average").ValueKind);
    }

    [Fact]
    public async Task GraphQlUsesTypedKnownPayloadAndUnknownFallback()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        const string query = """
            query {
              readings(filter: { metricKey: CO2 }) {
                totalCount
                items {
                  type
                  payload {
                    __typename
                    ... on AirQualityPayload { co2 pm25 humidity }
                  }
                }
              }
              paginated: readings(filter: { type: "air_quality" }, page: 1, pageSize: 1) {
                totalCount
                items { name }
              }
              locationAggregations: aggregationsByLocation(filter: { metricKey: CO2 }) {
                name
                count
                average
                trueCount
              }
              latestMotionValues: latestValues(metricKey: MOTION_DETECTED) {
                name
                numericValue
                booleanValue
              }
              unknownReadings: readings(filter: { type: "rainfall" }) {
                items {
                  payload {
                    __typename
                    ... on UnknownPayload { type json }
                  }
                }
              }
            }
            """;

        using HttpResponseMessage response = await PostGraphQlAsync(client, query);
        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);
        using JsonDocument document = JsonDocument.Parse(responseBody);
        JsonElement root = document.RootElement;
        Assert.False(root.TryGetProperty("errors", out JsonElement graphQlErrors), graphQlErrors.ToString());

        JsonElement knownItems = root.GetProperty("data").GetProperty("readings").GetProperty("items");
        Assert.Equal(3, knownItems.GetArrayLength());
        Assert.Equal("AirQualityPayload", knownItems[0].GetProperty("payload").GetProperty("__typename").GetString());
        Assert.True(knownItems[0].GetProperty("payload").TryGetProperty("co2", out _));

        JsonElement paginated = root.GetProperty("data").GetProperty("paginated");
        Assert.Equal(3, paginated.GetProperty("totalCount").GetInt64());
        Assert.Single(paginated.GetProperty("items").EnumerateArray());
        Assert.Equal("Garage", paginated.GetProperty("items")[0].GetProperty("name").GetString());

        JsonElement officeAggregation = root
            .GetProperty("data")
            .GetProperty("locationAggregations")
            .EnumerateArray()
            .Single(result => result.GetProperty("name").GetString() == "Office");
        Assert.Equal(2, officeAggregation.GetProperty("count").GetInt64());
        Assert.Equal(450, officeAggregation.GetProperty("average").GetDouble());
        Assert.Equal(JsonValueKind.Null, officeAggregation.GetProperty("trueCount").ValueKind);

        JsonElement latestMotionValues = root.GetProperty("data").GetProperty("latestMotionValues");
        Assert.Equal(2, latestMotionValues.GetArrayLength());
        Assert.True(latestMotionValues[0].GetProperty("booleanValue").ValueKind is JsonValueKind.True or JsonValueKind.False);
        Assert.Equal(JsonValueKind.Null, latestMotionValues[0].GetProperty("numericValue").ValueKind);

        JsonElement unknownItem = root
            .GetProperty("data")
            .GetProperty("unknownReadings")
            .GetProperty("items")[0];
        Assert.Equal("UnknownPayload", unknownItem.GetProperty("payload").GetProperty("__typename").GetString());
        Assert.Equal("rainfall", unknownItem.GetProperty("payload").GetProperty("type").GetString());
        using JsonDocument unknownPayload = JsonDocument.Parse(unknownItem.GetProperty("payload").GetProperty("json").GetString()!);
        Assert.Equal(1.25, unknownPayload.RootElement.GetProperty("millimeters").GetDouble());
    }

    [Fact]
    public async Task GraphQlRejectsNumericRangeWithoutMetricKey()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        const string query = "query { readings(filter: { minValue: 100 }) { totalCount } }";

        using HttpResponseMessage response = await PostGraphQlAsync(client, query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement error = Assert.Single(document.RootElement.GetProperty("errors").EnumerateArray());
        Assert.Contains("metric key is required", error.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("BAD_USER_INPUT", error.GetProperty("extensions").GetProperty("code").GetString());
    }

    [Fact]
    public async Task HealthEndpointChecksDatabaseConnectivity()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("healthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("connected", document.RootElement.GetProperty("database").GetString());
    }

    [Fact]
    public async Task DevelopmentApiDocumentationEndpointsAreAvailable()
    {
        using HttpClient client = fixture.Factory.CreateClient();
        using HttpResponseMessage swaggerResponse = await client.GetAsync("/swagger");
        using HttpResponseMessage graphqlResponse = await client.GetAsync("/graphql");

        Assert.Equal(HttpStatusCode.OK, swaggerResponse.StatusCode);
        Assert.Contains("openapi", await swaggerResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, graphqlResponse.StatusCode);
        Assert.Equal("text/html", graphqlResponse.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<HttpResponseMessage> PostGraphQlAsync(HttpClient client, string query)
    {
        using StringContent content = new(JsonSerializer.Serialize(new { query }), Encoding.UTF8, "application/json");
        return await client.PostAsync("/graphql", content);
    }
}
