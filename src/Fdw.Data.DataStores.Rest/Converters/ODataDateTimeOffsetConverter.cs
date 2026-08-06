using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM DateTimeOffset to CLR DateTimeOffset.
/// </summary>
[TypeOption(typeof(ODataConverters), "DateTimeOffset", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataDateTimeOffsetConverter()
    : DataTypeConverterBase(
        id: 12,
        name: "DateTimeOffset",
        sourceType: "DateTimeOffset",
        targetClrType: typeof(DateTimeOffset),
        dbType: DbType.DateTimeOffset)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(DateTimeOffset), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
