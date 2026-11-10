using System;
using System.Collections.Generic;
using FluentValidation.Results;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Validation;

/// <summary>
/// Turns a FluentValidation refusal into the messages of a failed result, keeping the code of every rule that has its own (FDW-4738).
/// </summary>
/// <remarks>
/// Why one method for the runner and for <see cref="ValidationResultExtensions"/>: a rule whose reason is a coded [MessageLogging]
/// message puts that message in <see cref="ValidationFailure.CustomState"/>, and it must reach the caller as itself, with its code and
/// client text, whichever way the refusal is turned into a result. Joining every failure into one message dropped the codes.
/// </remarks>
public static class ValidationFailureMessages
{
    /// <summary>Builds the messages of a refusal.</summary>
    /// <param name="result">The refused FluentValidation result.</param>
    /// <param name="rejected">Builds the one message that names the rules that have no coded message of their own, from their details (<c>property: message</c>, joined by <c>; </c>).</param>
    /// <returns>
    /// Each failed rule whose custom state is a message with a failure-band code (20000 and up) as that message, in order, then the one
    /// <paramref name="rejected"/> message when any rule has no such message. Empty when the result is valid.
    /// </returns>
    public static IReadOnlyList<IGenericMessage> From(ValidationResult result, Func<string, IGenericMessage> rejected)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (rejected == null)
        {
            throw new ArgumentNullException(nameof(rejected));
        }

        var messages = new List<IGenericMessage>();
        var uncoded = new List<string>();
        foreach (var error in result.Errors)
        {
            if (error.CustomState is IGenericMessage coded && coded.AsFailureMessage() is not null)
            {
                messages.Add(coded);
            }
            else
            {
                uncoded.Add($"{error.PropertyName}: {error.ErrorMessage}");
            }
        }

        if (uncoded.Count > 0)
        {
            messages.Add(rejected(string.Join("; ", uncoded)));
        }

        return messages;
    }
}
