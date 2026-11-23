using WeakAppWrapper.WebApi.GraphQL.Models;

namespace WeakAppWrapper.WebApi.GraphQL.Types;

public sealed class ReadingPayloadUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("ReadingPayload");
        descriptor.Type<AirQualityPayloadType>();
        descriptor.Type<EnergyPayloadType>();
        descriptor.Type<MotionPayloadType>();
        descriptor.Type<UnknownPayloadType>();
    }
}

public sealed class AirQualityPayloadType : ObjectType<AirQualityPayload>;

public sealed class EnergyPayloadType : ObjectType<EnergyPayload>;

public sealed class MotionPayloadType : ObjectType<MotionPayload>;

public sealed class UnknownPayloadType : ObjectType<UnknownPayload>;
