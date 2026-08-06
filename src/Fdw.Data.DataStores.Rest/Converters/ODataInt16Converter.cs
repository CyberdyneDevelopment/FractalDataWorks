using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Int16 to CLR short.
/// </summary>
[TypeOption(typeof(ODataConverters), "Int16", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataInt16Converter()
    : DataTypeConverterBase(
        id: 4,
        name: "Int16",
        sourceType: "Int16",
        targetClrType: typeof(short),
        dbType: DbType.Int16)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(short), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
