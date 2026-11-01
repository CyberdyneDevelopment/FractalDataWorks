using System;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Reads the typed values a graph states as text (FDW-1403): a variable's starting value and the value a Set Variable step sets.
/// </summary>
public static class GraphValues
{
    /// <summary>Reads a value of a stated type.</summary>
    /// <param name="typeName">The <c>DataTypes</c> option name.</param>
    /// <param name="text">The value as text; null is a null value of that type.</param>
    /// <param name="value">The typed value when the read succeeds; null for a null value.</param>
    /// <returns>Whether the type is a data type and reads the text; a failed read changes nothing.</returns>
    public static bool TryRead(string typeName, string? text, out object? value)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        value = null;
        var type = DataTypes.ByName(typeName);
        if (ReferenceEquals(type, DataTypes.NotFound))
        {
            return false;
        }

        if (text is null)
        {
            return true;
        }

        var converted = type.Convert(text);
        if (converted.IsFailure)
        {
            return false;
        }

        value = converted.Value;
        return true;
    }
}
