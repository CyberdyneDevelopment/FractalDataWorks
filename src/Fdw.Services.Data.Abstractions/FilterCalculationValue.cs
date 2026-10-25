using System;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// What a calculation yielded for a filter: the value, and the type it has, which the filter checks against the field.
/// </summary>
public sealed class FilterCalculationValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterCalculationValue"/> class.
    /// </summary>
    /// <param name="valueType">The type of the value: <see cref="decimal"/> for a number, <see cref="DateTimeOffset"/> for a date-time.</param>
    /// <param name="value">The value, boxed as <paramref name="valueType"/>.</param>
    public FilterCalculationValue(Type valueType, object value)
    {
        ValueType = valueType;
        Value = value;
    }

    /// <summary>Gets the type of the value.</summary>
    public Type ValueType { get; }

    /// <summary>Gets the value, boxed as <see cref="ValueType"/>.</summary>
    public object Value { get; }
}
