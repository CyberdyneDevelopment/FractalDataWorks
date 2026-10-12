using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for date-and-time values without timezone offset.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>datetime2</c>/<c>datetime</c>,
/// PostgreSQL <c>timestamp without time zone</c>, C# <see cref="System.DateTime"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "DateTime")]
public sealed class DateTimeDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeDataType"/> class.</summary>
    public DateTimeDataType()
        : base(name: "DateTime")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(DateTime);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.Moment(text) is { } held ? held.UtcDateTime : null;

    /// <inheritdoc />
    public override string Format(object value) => ((DateTime)value).ToString("O", System.Globalization.CultureInfo.InvariantCulture);
}
