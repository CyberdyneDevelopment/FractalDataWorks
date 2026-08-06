using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM DateTime to CLR DateTime.
/// </summary>
[TypeOption(typeof(ODataConverters), "DateTime", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataDateTimeConverter()
    : DataTypeConverterBase(
        id: 11,
        name: "DateTime",
        sourceType: "DateTime",
        targetClrType: typeof(DateTime),
        dbType: DbType.DateTime)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(DateTime), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
