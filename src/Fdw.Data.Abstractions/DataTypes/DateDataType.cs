using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for calendar date values (no time component).
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>date</c>, PostgreSQL <c>date</c>,
/// C# <c>System.DateOnly</c> (.NET 6+) or <c>System.DateTime</c> with time zeroed.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Date")]
public sealed class DateDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateDataType"/> class.</summary>
    public DateDataType()
        : base(name: "Date")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(DateTime);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.Moment(text) is { } held ? held.UtcDateTime.Date : null;

    /// <inheritdoc />
    public override string Format(object value) => ((DateTime)value).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
