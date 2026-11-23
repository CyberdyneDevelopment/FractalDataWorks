using System;
using Fdw.Collections;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A shape a value in a record can have, and how a condition reads it.</summary>
public interface IRecordValueShape : ITypeOption<int, RecordValueShapeBase>
{
    /// <summary>Whether the value has this shape.</summary>
    /// <param name="value">The record's value.</param>
    /// <returns><c>true</c> when it does.</returns>
    bool Matches(object value);

    /// <summary>Whether a value of this shape counts as true.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <returns><c>true</c> when it does.</returns>
    bool IsTrue(object value);

    /// <summary>Reads the value as a point in time.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <param name="moment">The point in time.</param>
    /// <returns><c>true</c> when a value of this shape is a point in time.</returns>
    bool TryMoment(object value, out DateTimeOffset moment);

    /// <summary>Reads the value as a number.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <param name="number">The number.</param>
    /// <returns><c>true</c> when the value is a number, or its text reads as one.</returns>
    bool TryDecimal(object value, out decimal number);
}
