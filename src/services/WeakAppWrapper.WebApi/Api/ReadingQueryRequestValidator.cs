using FluentValidation;
using WeakAppWrapper.WebApi.Models;

namespace WeakAppWrapper.WebApi.Api;

public sealed class ReadingQueryRequestValidator : AbstractValidator<ReadingQueryRequest>
{
    public ReadingQueryRequestValidator(IValidator<ReadingFilter> filterValidator)
    {
        RuleFor(request => request.Filter).SetValidator(filterValidator);

        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, ReadingQueryRequest.MaximumPageSize)
            .WithMessage($"Page size must be between 1 and {ReadingQueryRequest.MaximumPageSize}.");

        RuleFor(request => request.Page)
            .Must((request, page) => page <= int.MaxValue / request.PageSize)
            .When(request => request.PageSize is >= 1 and <= ReadingQueryRequest.MaximumPageSize)
            .WithMessage("The requested page is too large.");
    }
}
