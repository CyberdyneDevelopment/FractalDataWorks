using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Boolean to CLR bool.
/// </summary>
[TypeOption(typeof(ODataConverters), "Boolean", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataBooleanConverter()
    : DataTypeConverterBase(
        id: 6,
        name: "Boolean",
        sourceType: "Boolean",
        targetClrType: typeof(bool),
        dbType: DbType.Boolean)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(bool), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
