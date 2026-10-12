using System;
using System.Globalization;

namespace Fdw.Data.Abstractions;

/// <summary>
/// The reading rules the data types share, all over text: invariant culture, no thousands separators, whole numbers only for
/// whole-number types, and a point in time without an offset read as UTC. Every method answers <see langword="null"/> for text
/// that is not of the type; none throws for it.
/// </summary>
internal static class DataValueReader
{
    private const NumberStyles Whole = NumberStyles.AllowLeadingSign;
    private const NumberStyles Fractional = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
    private const NumberStyles Floating = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

    // Invariant text of a value: numbers and Guid through IFormattable, text as it is. Types whose text form is not their
    // default (Bool, the moments, Time) override Format on their own option.
    internal static string InvariantText(object value) =>
        value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString();

    // A whole number in [min, max].
    internal static long? WholeNumber(string text, long min, long max) =>
        long.TryParse(text, Whole, CultureInfo.InvariantCulture, out var parsed) && parsed >= min && parsed <= max ? parsed : (long?)null;

    internal static decimal? Decimal(string text) =>
        decimal.TryParse(text, Fractional, CultureInfo.InvariantCulture, out var parsed) ? parsed : (decimal?)null;

    internal static double? Double(string text) =>
        double.TryParse(text, Floating, CultureInfo.InvariantCulture, out var parsed) && !double.IsNaN(parsed) && !double.IsInfinity(parsed)
            ? parsed
            : (double?)null;

    internal static bool? Boolean(string text) =>
        bool.TryParse(text, out var parsed) ? parsed : (bool?)null;

    internal static Guid? Identifier(string text) =>
        Guid.TryParse(text, out var parsed) ? parsed : (Guid?)null;

    internal static DateTimeOffset? Moment(string text) =>
        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : (DateTimeOffset?)null;

    internal static TimeSpan? Duration(string text) =>
        TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var parsed) ? parsed : (TimeSpan?)null;
}
