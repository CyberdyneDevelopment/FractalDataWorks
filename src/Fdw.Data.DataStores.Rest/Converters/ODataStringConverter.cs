using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM String to CLR string.
/// </summary>
[TypeOption(typeof(ODataConverters), "String", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataStringConverter()
    : DataTypeConverterBase(
        id: 1,
        name: "String",
        sourceType: "String",
        targetClrType: typeof(string),
        dbType: DbType.String)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(string), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
