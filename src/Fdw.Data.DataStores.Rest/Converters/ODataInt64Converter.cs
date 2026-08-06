using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Int64 to CLR long.
/// </summary>
[TypeOption(typeof(ODataConverters), "Int64", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataInt64Converter()
    : DataTypeConverterBase(
        id: 3,
        name: "Int64",
        sourceType: "Int64",
        targetClrType: typeof(long),
        dbType: DbType.Int64)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(long), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
