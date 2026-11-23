using FluentValidation;
using FluentValidation.Results;

namespace WeakAppWrapper.WebApi.Api;

public static class FluentValidationEndpointFilter
{
    public static RouteHandlerBuilder ValidateWithFluentValidation<TRequest>(
        this RouteHandlerBuilder builder,
        params string[] ruleSets
    )
        where TRequest : class =>
        builder.AddEndpointFilter(
            async (context, next) =>
            {
                TRequest? request = context.Arguments.OfType<TRequest>().FirstOrDefault();
                if (request is null)
                {
                    return await next(context);
                }

                IValidator<TRequest> validator = context.HttpContext.RequestServices.GetRequiredService<
                    IValidator<TRequest>
                >();
                ValidationResult validation = await validator.ValidateAsync(
                    request,
                    options => options.IncludeRuleSets(ruleSets),
                    context.HttpContext.RequestAborted
                );

                return validation.IsValid
                    ? await next(context)
                    : Results.ValidationProblem(ToErrorDictionary(validation));
            }
        );

    private static Dictionary<string, string[]> ToErrorDictionary(ValidationResult validation) =>
        validation
            .Errors.GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "request" : error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
}
