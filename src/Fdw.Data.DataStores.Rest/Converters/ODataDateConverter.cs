using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Date to CLR DateTime.
/// </summary>
[TypeOption(typeof(ODataConverters), "Date", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataDateConverter()
    : DataTypeConverterBase(
        id: 13,
        name: "Date",
        sourceType: "Date",
        targetClrType: typeof(DateTime),
        dbType: DbType.Date)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(DateTime), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
