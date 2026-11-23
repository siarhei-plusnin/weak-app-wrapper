using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.GraphQL.Models;

public sealed record LatestMetricValuesRequest(MetricKey MetricKey, ReadingFilter Filter);
