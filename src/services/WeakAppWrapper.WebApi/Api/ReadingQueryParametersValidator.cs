using FluentValidation;
using FluentValidation.Results;
using WeakAppWrapper.WebApi.Mappings;
using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.Api;

public sealed class ReadingQueryParametersValidator : AbstractValidator<ReadingQueryParameters>
{
    public const string FilterRuleSet = "Filter";
    public const string ReadingsRuleSet = "Readings";
    public const string TimeBucketRuleSet = "TimeBucket";

    public ReadingQueryParametersValidator(IValidator<ReadingFilter> filterValidator)
    {
        RuleSet(
            FilterRuleSet,
            () =>
            {
                RuleFor(parameters => parameters)
                    .Must(parameters =>
                        string.IsNullOrWhiteSpace(parameters.MetricKey)
                        || MetricCatalog.TryParse(parameters.MetricKey, out _)
                    )
                    .WithMessage("Supported metric keys are co2, pm25, humidity, energy, and motionDetected.")
                    .OverridePropertyName("metricKey");

                RuleFor(parameters => parameters)
                    .CustomAsync(
                        async (parameters, context, cancellationToken) =>
                        {
                            ValidationResult result = await filterValidator.ValidateAsync(
                                parameters.ToReadingFilter(),
                                cancellationToken
                            );

                            foreach (ValidationFailure? failure in result.Errors)
                            {
                                context.AddFailure(failure.PropertyName, failure.ErrorMessage);
                            }
                        }
                    );
            }
        );

        RuleSet(
            ReadingsRuleSet,
            () =>
            {
                RuleFor(parameters => parameters)
                    .Must(parameters => (parameters.Page ?? 1) >= 1)
                    .WithMessage("Page must be greater than or equal to 1.")
                    .OverridePropertyName("page");

                RuleFor(parameters => parameters)
                    .Must(parameters =>
                    {
                        int pageSize = parameters.PageSize ?? ReadingQueryRequest.DefaultPageSize;
                        return pageSize is >= 1 and <= ReadingQueryRequest.MaximumPageSize;
                    })
                    .WithMessage($"Page size must be between 1 and {ReadingQueryRequest.MaximumPageSize}.")
                    .OverridePropertyName("pageSize");

                RuleFor(parameters => parameters)
                    .Must(parameters =>
                    {
                        int pageSize = parameters.PageSize ?? ReadingQueryRequest.DefaultPageSize;
                        int page = parameters.Page ?? 1;
                        return pageSize is < 1 or > ReadingQueryRequest.MaximumPageSize
                            || page <= int.MaxValue / pageSize;
                    })
                    .WithMessage("The requested page is too large.")
                    .OverridePropertyName("page");

                RuleFor(parameters => parameters)
                    .Must(parameters => ReadingQueryParser.TryParseSortField(parameters.SortBy, out _))
                    .WithMessage("Supported sort fields are capturedAt, name, type, and source.")
                    .OverridePropertyName("sortBy");

                RuleFor(parameters => parameters)
                    .Must(parameters => ReadingQueryParser.TryParseSortDirection(parameters.SortDirection, out _))
                    .WithMessage("Sort direction must be 'asc' or 'desc'.")
                    .OverridePropertyName("sortDirection");
            }
        );

        RuleSet(
            TimeBucketRuleSet,
            () =>
            {
                RuleFor(parameters => parameters)
                    .Must(parameters => ReadingQueryParser.TryParseTimeBucket(parameters.Bucket, out _))
                    .WithMessage("Supported time buckets are minute, hour, and day.")
                    .OverridePropertyName("bucket");
            }
        );
    }
}
