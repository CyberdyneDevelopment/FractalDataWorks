using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>time</c> — normalizes to <see cref="DataTypes.Time"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "time")]
public sealed class TimeType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeType"/> class.</summary>
    public TimeType()
        : base(
            id: 14,
            name: "time",
            description: "Time of day with configurable fractional-second precision.",
            abstractType: DataTypes.Time,
            isTemporal: true, maxScale: 7, defaultScale: 7)
    {
    }
}
