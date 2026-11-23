using FluentValidation;
using FluentValidation.Results;

namespace WeakAppWrapper.WebApi.GraphQL;

public static class GraphQLValidation
{
    public static async Task ValidateAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken
    )
    {
        ValidationResult result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        string message = string.Join(" ", result.Errors.Select(error => error.ErrorMessage));
        throw new GraphQLException(ErrorBuilder.New().SetMessage(message).SetCode("BAD_USER_INPUT").Build());
    }
}
