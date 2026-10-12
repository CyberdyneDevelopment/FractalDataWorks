using System;
using System.Globalization;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>Reads a stored value as an exact number the way the calculation engine does: a decimal or a whole number, never a floating-point value.</summary>
public static class SetNumbers
{
    private static readonly Type[] IntegerTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
    ];

    /// <summary>Reads one value.</summary>
    /// <param name="output">The output the read belongs to, for a refusal.</param>
    /// <param name="column">The column the value came from, for a refusal.</param>
    /// <param name="raw">The value.</param>
    /// <returns>The number, null when the value is empty, or the refusal.</returns>
    public static IGenericResult<decimal?> Read(string output, string column, object? raw)
    {
        if (raw is null || raw is DBNull)
        {
            return GenericResult<decimal?>.Success(null);
        }

        if (raw is decimal exact)
        {
            return GenericResult<decimal?>.Success(exact);
        }

        return Array.IndexOf(IntegerTypes, raw.GetType()) >= 0
            ? GenericResult<decimal?>.Success(Convert.ToDecimal(raw, CultureInfo.InvariantCulture))
            : SetFailure.Of<decimal?>("SetValueNotNumeric", output, $"column '{column}' holds a {raw.GetType().Name}; only decimal and whole-number values are exact");
    }
}
