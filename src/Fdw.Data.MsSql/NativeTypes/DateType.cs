using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>date</c> — normalizes to <see cref="DataTypes.Date"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "date")]
public sealed class DateType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DateType"/> class.</summary>
    public DateType()
        : base(
            id: 13,
            name: "date",
            description: "Date with no time component.",
            abstractType: DataTypes.Date,
            isTemporal: true)
    {
    }
}
