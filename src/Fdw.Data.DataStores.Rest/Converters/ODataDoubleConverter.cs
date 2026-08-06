using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Double to CLR double.
/// </summary>
[TypeOption(typeof(ODataConverters), "Double", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataDoubleConverter()
    : DataTypeConverterBase(
        id: 8,
        name: "Double",
        sourceType: "Double",
        targetClrType: typeof(double),
        dbType: DbType.Double)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(double), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
