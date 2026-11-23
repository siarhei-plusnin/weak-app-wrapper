using FluentValidation;
using FluentValidation.Results;
using WeakAppWrapper.WebApi.GraphQL.Models;
using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.GraphQL;

public sealed class LatestMetricValuesRequestValidator : AbstractValidator<LatestMetricValuesRequest>
{
    public LatestMetricValuesRequestValidator(IValidator<ReadingFilter> filterValidator)
    {
        RuleFor(request => request)
            .CustomAsync(
                async (request, context, cancellationToken) =>
                {
                    if (request.Filter.MetricKey is not null && request.Filter.MetricKey != request.MetricKey)
                    {
                        context.AddFailure(
                            "metricKey",
                            "The metric key in the filter must match the requested metric key."
                        );
                    }

                    ReadingFilter effectiveFilter = request.Filter with { MetricKey = request.MetricKey };

                    ValidationResult result = await filterValidator.ValidateAsync(effectiveFilter, cancellationToken);
                    foreach (ValidationFailure? failure in result.Errors)
                    {
                        context.AddFailure(failure.PropertyName, failure.ErrorMessage);
                    }
                }
            );
    }
}
