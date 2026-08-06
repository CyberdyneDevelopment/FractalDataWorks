using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Int32 to CLR int.
/// </summary>
[TypeOption(typeof(ODataConverters), "Int32", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataInt32Converter()
    : DataTypeConverterBase(
        id: 2,
        name: "Int32",
        sourceType: "Int32",
        targetClrType: typeof(int),
        dbType: DbType.Int32)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(int), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
