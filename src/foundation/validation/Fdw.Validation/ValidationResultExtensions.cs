using FluentValidation.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

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
    /// <param name="logger">The logger the refusal is logged to.</param>
    /// <returns>
    /// A success result if valid; otherwise a failure with a message for each rule that has a coded message of its own (<see cref="ValidationFailureMessages"/>),
    /// and one message naming the other rules, as <c>property: message</c> joined by <c>; </c>. The failure carries Validation-band numbers and client
    /// messages, so a host answers it with 400 and the reason (FDW-3539, FDW-4738).
    /// </returns>
    public static IGenericResult ToGenericResult(this ValidationResult result, ILogger logger)
        => result.IsValid
            ? GenericResult.Success()
            : GenericResult.Failure(ValidationFailureMessages.From(result, details => ValidationLog.InputRejected(logger, details)));

    /// <summary>
    /// Converts a FluentValidation <see cref="ValidationResult"/> to an <see cref="IGenericResult{T}"/>.
    /// Returns the provided value on success.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The FluentValidation result.</param>
    /// <param name="value">The value to return on success.</param>
    /// <param name="logger">The logger the refusal is logged to.</param>
    /// <returns>
    /// A success result with the value if valid; otherwise a failure with a message for each rule that has a coded message of its own, and one
    /// message naming the other rules, as <c>property: message</c> joined by <c>; </c>. The failure carries Validation-band numbers and client
    /// messages, so a host answers it with 400 and the reason.
    /// </returns>
    public static IGenericResult<T> ToGenericResult<T>(this ValidationResult result, T value, ILogger logger)
        => result.IsValid
            ? GenericResult<T>.Success(value)
            : GenericResult<T>.Failure(ValidationFailureMessages.From(result, details => ValidationLog.InputRejected(logger, details)));
}
