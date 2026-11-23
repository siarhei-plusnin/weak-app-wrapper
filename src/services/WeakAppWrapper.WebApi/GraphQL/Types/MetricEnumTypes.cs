using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.GraphQL.Types;

public sealed class MetricKeyType : EnumType<MetricKey>
{
    protected override void Configure(IEnumTypeDescriptor<MetricKey> descriptor)
    {
        descriptor.Name("MetricKey");
        descriptor.Value(MetricKey.Co2).Name("CO2");
        descriptor.Value(MetricKey.Pm25).Name("PM25");
        descriptor.Value(MetricKey.Humidity).Name("HUMIDITY");
        descriptor.Value(MetricKey.Energy).Name("ENERGY");
        descriptor.Value(MetricKey.MotionDetected).Name("MOTION_DETECTED");
    }
}

public sealed class ReadingSortFieldType : EnumType<ReadingSortField>
{
    protected override void Configure(IEnumTypeDescriptor<ReadingSortField> descriptor)
    {
        descriptor.Name("ReadingSortField");
        descriptor.Value(ReadingSortField.CapturedAt).Name("CAPTURED_AT");
        descriptor.Value(ReadingSortField.Name).Name("NAME");
        descriptor.Value(ReadingSortField.Type).Name("TYPE");
        descriptor.Value(ReadingSortField.Source).Name("SOURCE");
    }
}

public sealed class ReadingSortDirectionType : EnumType<ReadingSortDirection>
{
    protected override void Configure(IEnumTypeDescriptor<ReadingSortDirection> descriptor)
    {
        descriptor.Name("ReadingSortDirection");
        descriptor.Value(ReadingSortDirection.Ascending).Name("ASC");
        descriptor.Value(ReadingSortDirection.Descending).Name("DESC");
    }
}

public sealed class TimeBucketType : EnumType<TimeBucket>
{
    protected override void Configure(IEnumTypeDescriptor<TimeBucket> descriptor)
    {
        descriptor.Name("TimeBucket");
        descriptor.Value(TimeBucket.Minute).Name("MINUTE");
        descriptor.Value(TimeBucket.Hour).Name("HOUR");
        descriptor.Value(TimeBucket.Day).Name("DAY");
    }
}
