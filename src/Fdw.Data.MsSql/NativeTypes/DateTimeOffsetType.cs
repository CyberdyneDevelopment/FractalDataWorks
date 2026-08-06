using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>datetimeoffset</c> — normalizes to <see cref="DataTypes.DateTimeOffset"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "datetimeoffset")]
public sealed class DateTimeOffsetType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetType"/> class.</summary>
    public DateTimeOffsetType()
        : base(
            id: 12,
            name: "datetimeoffset",
            description: "Date and time with a timezone offset.",
            abstractType: DataTypes.DateTimeOffset,
            isTemporal: true, maxScale: 7, defaultScale: 7)
    {
    }
}
