using System;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Resolves the data type a data set field is declared with to the .NET type its values have.
/// </summary>
/// <remarks>
/// A field's <see cref="DataSetFieldConfiguration.TypeName"/> is the name of a <see cref="DataTypes"/> option,
/// and a query checks its filter values against that option's CLR type, so a value that is not of the declared
/// type is refused before it reaches a store. This is strict: the name is matched exactly, a name that is not an
/// option resolves to nothing, and the caller decides what an undeclared type means.
/// </remarks>
public static class DataSetFieldTypes
{
    /// <summary>Resolves a declared data type name to a .NET type.</summary>
    /// <param name="typeName">The declared name, such as <c>Guid</c>, <c>Int32</c> or <c>Decimal</c>.</param>
    /// <returns>The type, or <see langword="null"/> when the name is empty or is not a <see cref="DataTypes"/> option.</returns>
    public static Type? Resolve(string? typeName) => DataTypeNames.Find(typeName)?.ClrType;
}
