using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>datetime2</c> — normalizes to <see cref="DataTypes.DateTime"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "datetime2")]
public sealed class DateTime2Type : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTime2Type"/> class.</summary>
    public DateTime2Type()
        : base(
            id: 11,
            name: "datetime2",
            description: "Date and time with configurable fractional-second precision.",
            abstractType: DataTypes.DateTime,
            isTemporal: true, maxScale: 7, defaultScale: 7)
    {
    }
}
