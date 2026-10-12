using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for date-and-time values with timezone offset.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>datetimeoffset</c>,
/// PostgreSQL <c>timestamp with time zone</c>/<c>timestamptz</c>,
/// C# <see cref="System.DateTimeOffset"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "DateTimeOffset")]
public sealed class DateTimeOffsetDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetDataType"/> class.</summary>
    public DateTimeOffsetDataType()
        : base(name: "DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(DateTimeOffset);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.Moment(text);

    /// <inheritdoc />
    public override string Format(object value) => ((DateTimeOffset)value).ToString("O", System.Globalization.CultureInfo.InvariantCulture);
}
