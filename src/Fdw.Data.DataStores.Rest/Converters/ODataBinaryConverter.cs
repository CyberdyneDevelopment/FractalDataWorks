using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM Binary to CLR byte[].
/// </summary>
[TypeOption(typeof(ODataConverters), "Binary", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataBinaryConverter()
    : DataTypeConverterBase(
        id: 15,
        name: "Binary",
        sourceType: "Binary",
        targetClrType: typeof(byte[]),
        dbType: DbType.Binary)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(byte[]), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
