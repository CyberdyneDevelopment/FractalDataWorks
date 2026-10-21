using System;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Finds the <see cref="MsSqlNativeTypes"/> member for a type name as SQL Server reports it (<c>int</c>, <c>nvarchar</c>,
/// <c>datetime2</c>): the name a data store container field records as its <c>DataType</c>, and the one schema discovery
/// reads from the catalog.
/// </summary>
/// <remarks>
/// Why by <c>NativeName</c> and not <c>ByName</c>: an option's name is also the name of its generated static property, so the
/// types whose SQL names are C# keywords are named <c>Int</c>, <c>Decimal</c>, <c>Char</c> and <c>Float</c>, and
/// <c>ByName("int")</c> finds nothing. Every option states the name SQL Server uses as its <c>NativeName</c>, so that is the
/// key a name read from a store is looked up by (FDW-1588).
/// </remarks>
public static class MsSqlNativeTypeResolver
{
    /// <summary>Resolves a SQL Server type name to its native type.</summary>
    /// <param name="sqlTypeName">The type name as SQL Server reports it; case does not matter.</param>
    /// <returns>The native type, or <see cref="MsSqlNativeTypes.NotFound"/> when FDW has none for that name.</returns>
    public static IMsSqlDataType Resolve(string? sqlTypeName)
    {
        if (string.IsNullOrWhiteSpace(sqlTypeName))
            return MsSqlNativeTypes.NotFound;

        return MsSqlNativeTypes.All().FirstOrDefault(type => string.Equals(type.NativeName, sqlTypeName, StringComparison.OrdinalIgnoreCase))
            is { } found
                ? found
                : MsSqlNativeTypes.NotFound;
    }
}
