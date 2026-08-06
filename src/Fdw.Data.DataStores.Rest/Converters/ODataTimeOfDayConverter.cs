using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataStores.Rest;

/// <summary>
/// Converts OData EDM TimeOfDay to CLR TimeSpan.
/// </summary>
[TypeOption(typeof(ODataConverters), "TimeOfDay", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ODataTimeOfDayConverter()
    : DataTypeConverterBase(
        id: 14,
        name: "TimeOfDay",
        sourceType: "TimeOfDay",
        targetClrType: typeof(TimeSpan),
        dbType: DbType.Time)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue) =>
        dbValue is DBNull or null ? null : Convert.ChangeType(dbValue, typeof(TimeSpan), CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue;
}
