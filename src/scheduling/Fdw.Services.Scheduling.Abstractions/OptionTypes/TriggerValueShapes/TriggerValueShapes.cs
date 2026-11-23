using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>
/// The shapes a value in a trigger's configuration can have and how a trigger type reads each: an int, a long, a decimal, a double, a
/// float, a string, a DateTime, a DateTimeOffset and a bool.
/// </summary>
/// <remarks>A value is read by the shape that matches it; a value no shape matches, or none, reads as nothing.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TriggerValueShapeBase), typeof(ITriggerValueShape), typeof(TriggerValueShapes))]
public abstract partial class TriggerValueShapes : TypeCollectionBase<TriggerValueShapeBase, ITriggerValueShape>
{
    /// <summary>Reads a configuration value as a whole number, as the trigger types read one.</summary>
    /// <param name="value">The configuration value.</param>
    /// <param name="result">The number.</param>
    /// <returns><c>true</c> when the value is, or reads as, a whole number that fits an int.</returns>
    public static bool ReadInt(object? value, out int result)
    {
        result = 0;
        return Of(value) is { } shape && shape.TryInt(value!, out result);
    }

    /// <summary>Reads a configuration value as a whole number, as the Window trigger type reads one: as <see cref="ReadInt"/> does, except a float is not read.</summary>
    /// <param name="value">The configuration value.</param>
    /// <param name="result">The number.</param>
    /// <returns><c>true</c> when the value is, or reads as, a whole number that fits an int and is not a float.</returns>
    public static bool ReadWindowInt(object? value, out int result)
    {
        result = 0;
        return Of(value) is { } shape && shape.TryWindowInt(value!, out result);
    }

    /// <summary>Reads a configuration value as a whole number, as a stored trigger's own validation reads one: an int or the text of one.</summary>
    /// <param name="value">The configuration value.</param>
    /// <param name="result">The number.</param>
    /// <returns><c>true</c> when the value is an int or the text of one.</returns>
    public static bool ReadNarrowInt(object? value, out int result)
    {
        result = 0;
        return Of(value) is { } shape && shape.TryNarrowInt(value!, out result);
    }

    /// <summary>Reads a configuration value as a date and time.</summary>
    /// <param name="value">The configuration value.</param>
    /// <param name="result">The date and time.</param>
    /// <returns><c>true</c> when the value is, or reads as, a date and time.</returns>
    public static bool ReadDateTime(object? value, out DateTime result)
    {
        result = default;
        return Of(value) is { } shape && shape.TryDateTime(value!, out result);
    }

    /// <summary>Reads a configuration value as a true or false.</summary>
    /// <param name="value">The configuration value.</param>
    /// <param name="result">The flag.</param>
    /// <returns><c>true</c> when the value is, or reads as, a true or false.</returns>
    public static bool ReadBool(object? value, out bool result)
    {
        result = false;
        return Of(value) is { } shape && shape.TryBool(value!, out result);
    }

    private static ITriggerValueShape? Of(object? value)
        => value is null ? null : All().FirstOrDefault(shape => shape.Matches(value));
}
