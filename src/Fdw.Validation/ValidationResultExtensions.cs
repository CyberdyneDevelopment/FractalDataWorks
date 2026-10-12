using System.Linq;
using FluentValidation.Results;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Validation;

/// <summary>
/// Extension methods to bridge FluentValidation results into the FDW result pattern.
/// </summary>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Converts a FluentValidation <see cref="ValidationResult"/> to an <see cref="IGenericResult"/>.
    /// </summary>
    /// <param name="result">The FluentValidation result.</param>
    /// <returns>A success result if valid; a failure result with validation error messages otherwise.</returns>
    public static IGenericResult ToGenericResult(this ValidationResult result)
    {
        if (result.IsValid)
        {
            return GenericResult.Success();
        }

        var messages = result.Errors
            .Select(e => (IGenericMessage)GenericMessage.Create(
                MessageSeverity.Error,
                $"{e.PropertyName}: {e.ErrorMessage}",
                "VALIDATION",
                "Fdw.Validation"))
            .ToList();

        return GenericResult.Failure(messages);
    }

    /// <summary>
    /// Converts a FluentValidation <see cref="ValidationResult"/> to an <see cref="IGenericResult{T}"/>.
    /// Returns the provided value on success.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The FluentValidation result.</param>
    /// <param name="value">The value to return on success.</param>
    /// <returns>A success result with the value if valid; a failure result with validation error messages otherwise.</returns>
    public static IGenericResult<T> ToGenericResult<T>(this ValidationResult result, T value)
    {
        if (result.IsValid)
        {
            return GenericResult<T>.Success(value);
        }

        var messages = result.Errors
            .Select(e => (IGenericMessage)GenericMessage.Create(
                MessageSeverity.Error,
                $"{e.PropertyName}: {e.ErrorMessage}",
                "VALIDATION",
                "Fdw.Validation"))
            .ToList();

        return GenericResult<T>.Failure(messages);
    }
}
