using System;
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
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        foreach (var message in result.Messages)
        {
            if (message?.Code is not { Length: > 0 } text)
            {
                continue;
            }

            var dash = text.LastIndexOf('-');
            if (dash < 0 || !int.TryParse(text.Substring(dash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                continue;
            }

            if (number >= FirstFailureNumber)
            {
                return new FailureMessage(text, number, message);
            }
        }

        return null;
    }
}
