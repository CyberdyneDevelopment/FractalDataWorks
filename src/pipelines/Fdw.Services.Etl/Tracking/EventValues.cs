using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Fdw.Services.Etl.Tracking;

/// <summary>
/// Reads the values of a recorded event (FDW-3381). An event's data comes back from storage as whatever the store gives (a number, a text, a JSON element), so each value
/// is read by what it is. A value that is not there, or not of the kind asked for, is null: nothing is guessed.
/// </summary>
internal static class EventValues
{
    /// <summary>Reads a whole number.</summary>
    /// <param name="data">The event's data.</param>
    /// <param name="key">The value's name.</param>
    /// <returns>The number, or null.</returns>
    public static long? Number(IReadOnlyDictionary<string, object?>? data, string key) => Value(data, key) switch
    {
        long number => number,
        int number => number,
        JsonElement { ValueKind: JsonValueKind.Number } element when element.TryGetInt64(out var number) => number,
        string text when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number,
        _ => null,
    };

    /// <summary>Reads a true or false.</summary>
    /// <param name="data">The event's data.</param>
    /// <param name="key">The value's name.</param>
    /// <returns>The flag, or null.</returns>
    public static bool? Flag(IReadOnlyDictionary<string, object?>? data, string key) => Value(data, key) switch
    {
        bool flag => flag,
        JsonElement { ValueKind: JsonValueKind.True } => true,
        JsonElement { ValueKind: JsonValueKind.False } => false,
        _ => null,
    };

    /// <summary>Reads a time written round-trip.</summary>
    /// <param name="data">The event's data.</param>
    /// <param name="key">The value's name.</param>
    /// <returns>The time, or null.</returns>
    public static DateTimeOffset? Time(IReadOnlyDictionary<string, object?>? data, string key) => Value(data, key) switch
    {
        DateTimeOffset time => time,
        string text when DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) => time,
        JsonElement { ValueKind: JsonValueKind.String } element when DateTimeOffset.TryParse(element.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) => time,
        _ => null,
    };

    /// <summary>Reads a text.</summary>
    /// <param name="data">The event's data.</param>
    /// <param name="key">The value's name.</param>
    /// <returns>The text, or null.</returns>
    public static string? Text(IReadOnlyDictionary<string, object?>? data, string key) => Value(data, key) switch
    {
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        _ => null,
    };

    private static object? Value(IReadOnlyDictionary<string, object?>? data, string key) => data is not null && data.TryGetValue(key, out var value) ? value : null;
}
