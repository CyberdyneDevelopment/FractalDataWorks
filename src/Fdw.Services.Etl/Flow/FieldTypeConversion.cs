using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Converts a mapped value to the data type an input mapping states (FDW-2000, FDW-2009): the one place the types a mapping may convert to are read.
/// </summary>
/// <remarks>
/// Why the <c>DataTypes</c> collection and no list of its own: a mapping converts to the same types a constant is given, by the same exact names, so the
/// list lives in one place. A value that is already of the type's CLR type is kept; any other value is written as the invariant text of its own data type
/// and read by the target type, so a value the target cannot read is refused and no value is invented for it.
/// </remarks>
public static class FieldTypeConversion
{
    /// <summary>Gets the names of the types a mapping may convert to, in name order.</summary>
    public static IReadOnlyList<string> Supported => [.. DataTypes.All().Select(type => type.Name).Order(StringComparer.Ordinal)];

    /// <summary>Whether a name is a type a mapping may convert to.</summary>
    /// <param name="targetType">The data type's name, matched exactly.</param>
    /// <returns><see langword="true"/> for the name of a data type.</returns>
    public static bool IsSupported(string targetType) => DataTypeNames.Find(targetType) is not null;

    /// <summary>Converts a value to a data type.</summary>
    /// <param name="value">The value, not null.</param>
    /// <param name="targetType">The data type's name, matched exactly.</param>
    /// <param name="converted">Receives the converted value when the conversion worked.</param>
    /// <param name="reason">Receives why it did not when it failed.</param>
    /// <returns><see langword="true"/> when the value was converted.</returns>
    public static bool TryConvert(object value, string targetType, out object? converted, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetType);
        converted = null;
        var target = DataTypeNames.Find(targetType);
        if (target is null)
        {
            reason = $"'{targetType}' is not a data type";
            return false;
        }

        if (value.GetType() == target.ClrType)
        {
            converted = value;
            reason = null;
            return true;
        }

        var text = TextOf(value, out reason);
        if (text is null)
        {
            return false;
        }

        var read = target.Convert(text);
        if (read.IsFailure || read.Value is null)
        {
            reason = $"'{text}' is not a value of {target.Name}";
            return false;
        }

        converted = read.Value;
        return true;
    }

    // The text a value has in its own data type, the form any data type reads. A value no data type reads has none, and is refused.
    private static string? TextOf(object value, out string? reason)
    {
        reason = null;
        if (value is string text)
        {
            return text;
        }

        var own = DataTypeNames.Reading(value.GetType());
        if (own.Count == 0)
        {
            reason = $"no data type reads a {value.GetType().Name}";
            return null;
        }

        return own[0].Format(value);
    }
}
