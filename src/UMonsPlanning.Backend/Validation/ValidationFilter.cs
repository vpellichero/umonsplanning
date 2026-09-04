using FluentValidation;
using FluentValidation.Results;

namespace UMonsPlanning.Backend.Validation;

/// <summary>
/// Centralized FluentValidation execution for Minimal APIs (docs/ai/backend-dotnet.md §Validation).
/// Deliberately wired by hand rather than through the (deprecated) <c>FluentValidation.AspNetCore</c> package.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        T? argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return await next(context).ConfigureAwait(false);
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        ValidationResult result = await validator.ValidateAsync(argument).ConfigureAwait(false);

        if (!result.IsValid)
        {
            return Results.ValidationProblem(ToErrorDictionary(result));
        }

        return await next(context).ConfigureAwait(false);
    }

    private static IDictionary<string, string[]> ToErrorDictionary(ValidationResult result) =>
        result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
