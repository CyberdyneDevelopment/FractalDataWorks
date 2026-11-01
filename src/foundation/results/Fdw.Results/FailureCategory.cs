using System;
using System.Globalization;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Reads the category of a failed result: what kind of failure it is, from the number of its code (FDW-2338).
/// </summary>
/// <remarks>
/// A code's category is its number divided by 10000. A result built from a result code carries that code (or one in its chain); a
/// result built from a <c>[MessageLogging]</c> message carries no code and only the message's <c>PREFIX-NNNNN</c> text, whose number
/// encodes the category the same way. Both are read, the result's own code first, so a caller asking "was this missing" gets the same
/// answer whichever way the failure was made. Success and progress numbers (below 20000) are not failures and are skipped.
/// </remarks>
public static class FailureCategory
{
    private const int CategoryBand = 10000;
    private const int FirstFailureNumber = 20000;

    /// <summary>Reads the category of a failed result.</summary>
    /// <param name="result">The result.</param>
    /// <returns>The category of the failure; null when the result carries no categorized code.</returns>
    public static IResultCategory? Of(IGenericResult result)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        if (result.Code is { } code && code.Id >= FirstFailureNumber)
            return ResultCategories.ById(code.Id / CategoryBand);

        foreach (var chained in result.CodeChain)
        {
            if (chained is not null && chained.Id >= FirstFailureNumber)
                return ResultCategories.ById(chained.Id / CategoryBand);
        }

        foreach (var message in result.Messages)
        {
            if (message?.Code is not { Length: > 0 } text)
                continue;

            var dash = text.LastIndexOf('-');
            if (dash >= 0
                && int.TryParse(text.Substring(dash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                && number >= FirstFailureNumber)
            {
                return ResultCategories.ById(number / CategoryBand);
            }
        }

        return null;
    }

    /// <summary>Says whether a failed result is a "missing": the thing asked for is not there.</summary>
    /// <param name="result">The result.</param>
    /// <returns>True when the failure is in the missing category.</returns>
    public static bool IsMissing(IGenericResult result) => ReferenceEquals(Of(result), ResultCategories.Missing);
}
