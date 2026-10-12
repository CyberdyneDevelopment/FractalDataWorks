using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Finds a <see cref="DataTypes"/> option by the name a field states, or by the CLR type it reads (FDW-1159).
/// </summary>
/// <remarks>
/// A field's data type is the option's <see cref="IDataType.Name"/>: <c>Int32</c>, <c>String</c>,
/// <c>DateTimeOffset</c>. The name is matched exactly, because the stored value is what every reader compares
/// against; <c>int</c>, <c>INT32</c> and <c>System.Int32</c> are not names. A name that is not an option finds
/// nothing, and the caller decides what that means.
/// </remarks>
public static class DataTypeNames
{
    /// <summary>Finds the option a field's stated data type names.</summary>
    /// <param name="name">The stated name.</param>
    /// <returns>The option, or <see langword="null"/> when the name is empty or is not an option's name.</returns>
    public static IDataType? Find(string? name) =>
        name is { Length: > 0 }
            ? DataTypes.All().FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.Ordinal))
            : null;

    /// <summary>Lists the options whose values are of a CLR type, in name order.</summary>
    /// <param name="clrType">The CLR type.</param>
    /// <returns>
    /// Every option that reads <paramref name="clrType"/>. More than one for a CLR type two options share
    /// (<c>Date</c> and <c>DateTime</c> both read <see cref="DateTime"/>), none for a type no option reads.
    /// </returns>
    public static IReadOnlyList<IDataType> Reading(Type clrType)
    {
        if (clrType is null) throw new ArgumentNullException(nameof(clrType));
        return DataTypes.All().Where(option => option.ClrType == clrType).OrderBy(option => option.Name, StringComparer.Ordinal).ToList();
    }
}
