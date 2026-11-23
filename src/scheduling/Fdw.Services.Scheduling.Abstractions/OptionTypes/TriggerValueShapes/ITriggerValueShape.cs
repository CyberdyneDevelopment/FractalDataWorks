using System;
using Fdw.Collections;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A shape a value in a trigger's configuration can have, and how a trigger type reads it.</summary>
public interface ITriggerValueShape : ITypeOption<int, TriggerValueShapeBase>
{
    /// <summary>Whether the value has this shape.</summary>
    /// <param name="value">The configuration value.</param>
    /// <returns><c>true</c> when it does.</returns>
    bool Matches(object value);

    /// <summary>Reads the value as a whole number, as the trigger types read one.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <param name="result">The number.</param>
    /// <returns><c>true</c> when a value of this shape is a whole number that fits an int.</returns>
    bool TryInt(object value, out int result);

    /// <summary>Reads the value as a whole number, as the Window trigger type reads one: as <see cref="TryInt"/> does, except a float is not read.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <param name="result">The number.</param>
    /// <returns><c>true</c> when a value of this shape is a whole number that fits an int and is not a float.</returns>
    bool TryWindowInt(object value, out int result);

    /// <summary>Reads the value as a whole number, as a stored trigger's own validation reads one: an int or the text of one.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <param name="result">The number.</param>
    /// <returns><c>true</c> when a value of this shape is an int or the text of one.</returns>
    bool TryNarrowInt(object value, out int result);

    /// <summary>Reads the value as a date and time.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <param name="result">The date and time.</param>
    /// <returns><c>true</c> when a value of this shape is, or reads as, a date and time.</returns>
    bool TryDateTime(object value, out DateTime result);

    /// <summary>Reads the value as a true or false.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <param name="result">The flag.</param>
    /// <returns><c>true</c> when a value of this shape is, or reads as, a true or false.</returns>
    bool TryBool(object value, out bool result);
}
