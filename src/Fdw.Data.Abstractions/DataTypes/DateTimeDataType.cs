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
        : base(id: 9, name: "DateTime")
    {
    }
}
