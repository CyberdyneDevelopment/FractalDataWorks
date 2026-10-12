using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Equality of the key values of two records: element by element, numbers by value whatever their type, strings
/// ignoring case (the way the Aggregate transform groups), everything else by its own equality. A null equals a null and
/// nothing else.
/// </summary>
internal sealed class KeyValuesComparer : IEqualityComparer<object?[]>
{
    /// <summary>Turns a record's key values into the form this comparer compares.</summary>
    /// <param name="values">The key values, in key order.</param>
    /// <returns>The normalised values.</returns>
    public static object?[] Normalise(object?[] values)
    {
        var normalised = new object?[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            normalised[index] = NormaliseValue(values[index]);
        }

        return normalised;
    }

    /// <inheritdoc />
    public bool Equals(object?[]? x, object?[]? y)
    {
        if (x is null || y is null || x.Length != y.Length)
        {
            return false;
        }

        for (var index = 0; index < x.Length; index++)
        {
            if (!ValueEquals(x[index], y[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public int GetHashCode(object?[] obj)
    {
        var hash = new HashCode();
        foreach (var value in obj)
        {
            hash.Add(value is string text ? StringComparer.OrdinalIgnoreCase.GetHashCode(text) : value?.GetHashCode() ?? 0);
        }

        return hash.ToHashCode();
    }

    private static bool ValueEquals(object? left, object? right)
    {
        if (left is string leftText && right is string rightText)
        {
            return string.Equals(leftText, rightText, StringComparison.OrdinalIgnoreCase);
        }

        return Equals(left, right);
    }

    private static object? NormaliseValue(object? value)
    {
        if (value is null || !OrderValueComparer.IsNumber(value))
        {
            return value;
        }

        if (value is float or double)
        {
            var real = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return double.IsFinite(real) && Math.Abs(real) <= (double)decimal.MaxValue ? (decimal)real : real;
        }

        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }
}
