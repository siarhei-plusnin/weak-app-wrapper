using WeakAppWrapper.WebApi.GraphQL.Models;

namespace WeakAppWrapper.WebApi.GraphQL.Types;

public sealed class ReadingGraphType : ObjectType<GraphReading>
{
    protected override void Configure(IObjectTypeDescriptor<GraphReading> descriptor)
    {
        descriptor.Name("Reading");
        descriptor.Field(reading => reading.Payload).Type<ReadingPayloadUnionType>();
    }
}

public sealed class ReadingPageGraphType : ObjectType<ReadingPageGraph>
{
    protected override void Configure(IObjectTypeDescriptor<ReadingPageGraph> descriptor) =>
        descriptor.Name("ReadingPage");
}
