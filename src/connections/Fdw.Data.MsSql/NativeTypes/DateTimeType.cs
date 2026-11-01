using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>datetime</c> — normalizes to <see cref="DataTypes.DateTime"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "datetime")]
public sealed class DateTimeType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeType"/> class.</summary>
    public DateTimeType()
        : base(
            name: "datetime",
            description: "Date and time, accurate to about three milliseconds.",
            abstractType: DataTypes.DateTime,
            isTemporal: true)
    {
    }
}
