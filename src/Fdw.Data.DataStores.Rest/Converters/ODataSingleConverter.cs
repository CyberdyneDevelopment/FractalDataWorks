using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Single to CLR float.
/// </summary>
[TypeOption(typeof(ODataConverters), "Single", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataSingleConverter()
    : DataTypeConverterBase(
        id: 9,
        name: "Single",
        sourceType: "Single",
        targetClrType: typeof(float),
        dbType: DbType.Single)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(float), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
