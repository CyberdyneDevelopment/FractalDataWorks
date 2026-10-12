using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The SQL Server data types this server writes.
/// </summary>
/// <remarks>
/// The set is SQL Server's, not this server's to grow, so every option is declared in this assembly.
/// A column's type is found by the .NET type its values hold through <see cref="ForClrType"/>; a .NET type
/// no option holds has no SQL type here, and a column of it cannot be described.
/// </remarks>
[TypeCollection(typeof(SqlDataTypeBase), typeof(ISqlDataType), typeof(SqlDataTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class SqlDataTypes : TypeCollectionBase<SqlDataTypeBase, ISqlDataType>
{
    /// <summary>Finds the SQL type a .NET type's values are written as.</summary>
    /// <param name="clrType">The .NET type.</param>
    /// <param name="dataType">The SQL type, when one holds it.</param>
    /// <returns>True when a SQL type holds values of that .NET type.</returns>
    public static bool ForClrType(Type clrType, [NotNullWhen(true)] out ISqlDataType? dataType)
    {
        ArgumentNullException.ThrowIfNull(clrType);
        dataType = All().FirstOrDefault(t => t.Holds(clrType));
        return dataType is not null;
    }
}
