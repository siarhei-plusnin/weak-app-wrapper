using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.GraphQL.Types;

public sealed class AggregationGraphType : ObjectType<AggregationResponse>
{
    protected override void Configure(IObjectTypeDescriptor<AggregationResponse> descriptor) =>
        descriptor.Name("ReadingAggregation");
}

public sealed class LatestMetricValueGraphType : ObjectType<LatestMetricValueResponse>
{
    protected override void Configure(IObjectTypeDescriptor<LatestMetricValueResponse> descriptor) =>
        descriptor.Name("LatestMetricValue");
}
