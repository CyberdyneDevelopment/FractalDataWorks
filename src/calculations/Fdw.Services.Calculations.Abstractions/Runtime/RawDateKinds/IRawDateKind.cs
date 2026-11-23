using System;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>One kind of value a record can hold in a field a calculation reads as a date, and what reading it means.</summary>
public interface IRawDateKind : ITypeOption<int, RawDateKindBase>
{
    /// <summary>Tells whether a record's field value is of this kind.</summary>
    /// <param name="raw">The value; null when the field holds none.</param>
    /// <returns>True when this kind holds the value.</returns>
    bool Holds(object? raw);

    /// <summary>Reads a value of this kind as a calendar date.</summary>
    /// <param name="raw">A value this kind <see cref="Holds"/>.</param>
    /// <param name="field">The field name, for a failure.</param>
    /// <param name="state">The run state a failure is recorded in.</param>
    /// <param name="node">The index of the reading node.</param>
    /// <returns>The date, or the default date when the read failed.</returns>
    DateOnly Read(object? raw, string field, CalculationRunState state, int node);
}
