using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fdw.Results;

/// <summary>
/// Reads which message of a failed result says why it failed (FDW-3302).
/// </summary>
/// <remarks>
/// Why one reader: a failure can carry several messages (progress logged on the way, the coded failure, the exception chain behind
/// it). <see cref="IGenericResult.CurrentMessage"/> is the last of them; the HTTP answer is the first failure-category one. Two readers
/// that pick different messages tell the API caller and the pipeline run different things about the same failure, so both read it here.
/// </remarks>
public static class FailureMessageExtensions
{
    // Why 20000: a code's number divided by 10000 is its category, and 10000 to 19999 are success and progress.
    private const int FirstFailureNumber = 20000;

    /// <summary>
    /// Gets the first message on <paramref name="result"/> whose code number is in a failure category (20000 and up), parsed from the
    /// generator's "PREFIX-NNNNN" form.
    /// </summary>
    /// <param name="result">The result to read.</param>
    /// <returns>
    /// The message, or <see langword="null"/> when the result carries none. Success and progress codes are skipped: a failure that
    /// logged progress on its way must not be read as a success.
    /// </returns>
    public static FailureMessage? FirstFailureMessage(this IGenericResult result)
    {
        var failures = result.FailureMessages();
        return failures.Count > 0 ? failures[0] : null;
    }

    /// <summary>
    /// Gets every message on <paramref name="result"/> whose code number is in a failure category (20000 and up), in the order the
    /// result holds them.
    /// </summary>
    /// <param name="result">The result to read.</param>
    /// <returns>The messages; empty when the result carries none. The first is the one <see cref="FirstFailureMessage"/> returns.</returns>
    /// <remarks>
    /// Why all of them: a refusal by a validator carries one message for each rule that failed, and the answer to the caller names
    /// every one of them while taking its code and status from the first (FDW-4738).
    /// </remarks>
    public static IReadOnlyList<FailureMessage> FailureMessages(this IGenericResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var failures = new List<FailureMessage>();
        foreach (var message in result.Messages)
        {
            if (message?.AsFailureMessage() is { } failure)
            {
                failures.Add(failure);
            }
        }

        return failures;
    }

    /// <summary>
    /// Reads <paramref name="message"/> as a failure message: its code in the generator's "PREFIX-NNNNN" form with a number of 20000 or more.
    /// </summary>
    /// <param name="message">The message to read.</param>
    /// <returns>The failure message, or <see langword="null"/> when the message has no code, a code of another form, or a success or progress number.</returns>
    public static FailureMessage? AsFailureMessage(this Fdw.Messages.IGenericMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (message.Code is not { Length: > 0 } text)
        {
            return null;
        }

        var dash = text.LastIndexOf('-');
        if (dash < 0 || !int.TryParse(text.Substring(dash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        return number >= FirstFailureNumber ? new FailureMessage(text, number, message) : null;
    }

    /// <summary>
    /// Gets the message a failed result carries: its most recent one, the same message
    /// <see cref="IGenericResult.CurrentMessage"/> names. A failed <c>GenericResult</c> always has one, so a reader never supplies
    /// a default message (FDW-3580).
    /// </summary>
    /// <param name="result">A failed result.</param>
    /// <returns>The message; read <see cref="Fdw.Messages.IGenericMessage.Message"/> for its text and Code for its code.</returns>
    /// <exception cref="InvalidOperationException">The result succeeded.</exception>
    /// <remarks>
    /// Why the most recent message: it is what every reader read through CurrentMessage before, so the text each one reports does
    /// not change. <see cref="FirstFailureMessage"/> is the reader that picks the coded reason the HTTP answer reports.
    /// </remarks>
    public static Fdw.Messages.IGenericMessage FailureReason(this IGenericResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result has no failure reason.");
        }

        // Why UnstatedFailure: GenericResult never fails without a message, but another IGenericResult implementer can, and a
        // reader on a failure path must not throw (Mike, ee99604a).
        return result.Messages.Count > 0
            ? result.Messages[result.Messages.Count - 1]
            : UnstatedFailure.Message();
    }

    /// <summary>
    /// Gets why a result has no value to read: its failure reason when it failed, or the named <see cref="MissingValue"/> failure
    /// when it succeeded with a null value. The reader for a guard of the form <c>!result.IsSuccess || result.Value is null</c>.
    /// </summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="result">A result that failed or carries no value.</param>
    /// <returns>The message; read <see cref="Fdw.Messages.IGenericMessage.Message"/> for its text and Code for its code.</returns>
    /// <exception cref="InvalidOperationException">The result succeeded with a value.</exception>
    public static Fdw.Messages.IGenericMessage NoValueReason<T>(this IGenericResult<T> result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (!result.IsSuccess)
        {
            return result.FailureReason();
        }

        return result.Value is null
            ? MissingValue.Message(typeof(T).Name)
            : throw new InvalidOperationException("The result succeeded with a value: it has no reason to give.");
    }
}
