using System;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Builders;

/// <summary>
/// Names the <see cref="DataTypes"/> option a builder's field takes from a CLR type (FDW-1159).
/// </summary>
/// <remarks>
/// A field stores its data type as the option's name, never as a CLR type name. A CLR type that no option reads,
/// or that more than one reads (<see cref="DateTime"/> is read by both <c>Date</c> and <c>DateTime</c>), has no
/// single name, so it is refused; the caller names the option itself.
/// </remarks>
internal static class FieldDataType
{
    /// <summary>Finds the name of the one option that reads a CLR type.</summary>
    /// <param name="type">The CLR type.</param>
    /// <returns>The option's name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">No option, or more than one, reads <paramref name="type"/>.</exception>
    internal static string NameFor(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var reading = DataTypeNames.Reading(type);
        if (reading.Count == 1)
        {
            return reading[0].Name;
        }

        throw new ArgumentException(
            reading.Count == 0
                ? $"No data type reads values of {type}; state the field's data type by its DataTypes option."
                : $"{type} is read by more than one data type ({string.Join(", ", reading.Select(option => option.Name))}); state the field's data type by its DataTypes option.",
            nameof(type));
    }
}
