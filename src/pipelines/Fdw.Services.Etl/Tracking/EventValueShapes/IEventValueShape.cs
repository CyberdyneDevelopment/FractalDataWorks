using System;
using Fdw.Collections;

namespace Fdw.Services.Etl.Tracking;

/// <summary>A shape a value in a run event's data can have, and the figures it reads as.</summary>
public interface IEventValueShape : ITypeOption<int, EventValueShapeBase>
{
    /// <summary>Whether the value has this shape.</summary>
    /// <param name="value">The value in the event's data.</param>
    /// <returns><c>true</c> when it does.</returns>
    bool Matches(object value);

    /// <summary>Reads the value as a whole number.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <returns>The number, or <c>null</c> when a value of this shape is not a whole number.</returns>
    long? Number(object value);

    /// <summary>Reads the value as a true or false.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <returns>The flag, or <c>null</c> when a value of this shape is not a flag.</returns>
    bool? Flag(object value);

    /// <summary>Reads the value as a time written round-trip.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <returns>The time, or <c>null</c> when a value of this shape is not a time.</returns>
    DateTimeOffset? Time(object value);

    /// <summary>Reads the value as a text.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <returns>The text, or <c>null</c> when a value of this shape is not a text.</returns>
    string? Text(object value);
}
