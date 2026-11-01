using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>smalldatetime</c> — normalizes to <see cref="DataTypes.DateTime"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "smalldatetime")]
public sealed class SmallDateTimeType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="SmallDateTimeType"/> class.</summary>
    public SmallDateTimeType()
        : base(
            name: "smalldatetime",
            description: "Date and time, accurate to the minute.",
            abstractType: DataTypes.DateTime,
            isTemporal: true)
    {
    }
}
