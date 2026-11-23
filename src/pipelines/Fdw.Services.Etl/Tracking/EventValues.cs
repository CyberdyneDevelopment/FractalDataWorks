using System;
using System.Collections.Generic;

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
    public static long? Number(IReadOnlyDictionary<string, object?>? data, string key) => EventValueShapes.Of(Value(data, key)) is { } shape ? shape.Number(Value(data, key)!) : null;

    /// <summary>Reads a true or false.</summary>
    /// <param name="data">The event's data.</param>
    /// <param name="key">The value's name.</param>
    /// <returns>The flag, or null.</returns>
    public static bool? Flag(IReadOnlyDictionary<string, object?>? data, string key) => EventValueShapes.Of(Value(data, key)) is { } shape ? shape.Flag(Value(data, key)!) : null;

    /// <summary>Reads a time written round-trip.</summary>
    /// <param name="data">The event's data.</param>
    /// <param name="key">The value's name.</param>
    /// <returns>The time, or null.</returns>
    public static DateTimeOffset? Time(IReadOnlyDictionary<string, object?>? data, string key) => EventValueShapes.Of(Value(data, key)) is { } shape ? shape.Time(Value(data, key)!) : null;

    /// <summary>Reads a text.</summary>
    /// <param name="data">The event's data.</param>
    /// <param name="key">The value's name.</param>
    /// <returns>The text, or null.</returns>
    public static string? Text(IReadOnlyDictionary<string, object?>? data, string key) => EventValueShapes.Of(Value(data, key)) is { } shape ? shape.Text(Value(data, key)!) : null;

    private static object? Value(IReadOnlyDictionary<string, object?>? data, string key) => data is not null && data.TryGetValue(key, out var value) ? value : null;
}
