using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Decimal to CLR decimal.
/// </summary>
[TypeOption(typeof(ODataConverters), "Decimal", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataDecimalConverter()
    : DataTypeConverterBase(
        id: 7,
        name: "Decimal",
        sourceType: "Decimal",
        targetClrType: typeof(decimal),
        dbType: DbType.Decimal)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(decimal), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
