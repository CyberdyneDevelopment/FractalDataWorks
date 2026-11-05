using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fdw.Operations.Thresholds;

/// <summary>Reads a stored value back from its text and data type (FDW-3935): the one reader for a criteria condition and a threshold parameter.</summary>
internal static class ThresholdValueText
{
    // The data types Read understands, for a validator that refuses a row naming any other; keep it in step with the switch below.
    public static readonly IReadOnlyList<string> DataTypes = ["Int64", "Double", "Boolean", "TimeSpan", "DateTimeOffset", "String"];

    /// <summary>Reads a value.</summary>
    /// <param name="dataType">Int64, Double, String, Boolean, TimeSpan or DateTimeOffset.</param>
    /// <param name="text">The value as text in the invariant culture.</param>
    /// <returns>The typed value; null when the text is not a value of the data type or the data type is not one of the six.</returns>
    public static object? Read(string dataType, string text) => dataType switch
    {
        "Int64" => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole) ? whole : null,
        "Double" => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null,
        "Boolean" => bool.TryParse(text, out var flag) ? flag : null,
        "TimeSpan" => TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var span) ? span : null,
        "DateTimeOffset" => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var moment) ? moment : null,
        "String" => text,
        _ => null,
    };
}
