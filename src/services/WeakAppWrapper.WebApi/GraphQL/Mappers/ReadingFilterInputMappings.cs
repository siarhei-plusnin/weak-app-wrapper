using WeakAppWrapper.WebApi.GraphQL.Models;
using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.GraphQL.Mappers;

public static class ReadingFilterInputMappings
{
    extension(ReadingFilterInput input)
    {
        public ReadingFilter ToReadingFilter() =>
            new(
                input.Source,
                input.Name,
                input.Type,
                input.MetricKey,
                input.From,
                input.To,
                input.MinValue,
                input.MaxValue
            );
    }
}
