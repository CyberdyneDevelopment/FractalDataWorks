using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Guid to CLR Guid.
/// </summary>
[TypeOption(typeof(ODataConverters), "Guid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataGuidConverter()
    : DataTypeConverterBase(
        id: 10,
        name: "Guid",
        sourceType: "Guid",
        targetClrType: typeof(Guid),
        dbType: DbType.Guid)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(Guid), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
