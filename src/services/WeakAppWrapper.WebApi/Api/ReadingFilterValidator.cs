using FluentValidation;
using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.Api;

public sealed class ReadingFilterValidator : AbstractValidator<ReadingFilter>
{
    public ReadingFilterValidator()
    {
        RuleFor(filter => filter.To)
            .Must((filter, to) => to is null || filter.From is null || filter.From <= to)
            .WithMessage("The 'to' timestamp must be greater than or equal to 'from'.")
            .OverridePropertyName("to");

        RuleFor(filter => filter.MinValue)
            .Must(value => value is null || double.IsFinite(value.Value))
            .WithMessage("The minimum value must be a finite number.")
            .OverridePropertyName("minValue");

        RuleFor(filter => filter.MaxValue)
            .Must(value => value is null || double.IsFinite(value.Value))
            .WithMessage("The maximum value must be a finite number.")
            .OverridePropertyName("maxValue");

        RuleFor(filter => filter.MinValue)
            .Must(
                (filter, minimum) =>
                    minimum is null || filter.MaxValue is null || minimum.Value <= filter.MaxValue.Value
            )
            .WithMessage("The minimum value must be less than or equal to the maximum value.")
            .OverridePropertyName("minValue");

        RuleFor(filter => filter.MetricKey)
            .Must(metricKey => metricKey is not null)
            .When(HasNumericRange)
            .WithMessage("A metric key is required when filtering by a numeric value range.")
            .OverridePropertyName("metricKey");

        RuleFor(filter => filter.MetricKey)
            .Must(IsNumericMetric)
            .When(filter => HasNumericRange(filter) && filter.MetricKey is not null)
            .WithMessage("Numeric range filters are only supported for numeric metrics.")
            .OverridePropertyName("metricKey");
    }

    private static bool HasNumericRange(ReadingFilter filter) =>
        filter.MinValue is not null || filter.MaxValue is not null;

    private static bool IsNumericMetric(MetricKey? metricKey) =>
        metricKey is MetricKey value
        && Enum.IsDefined(value)
        && MetricCatalog.GetDefinition(value).ValueKind == MetricValueKind.Numeric;
}
