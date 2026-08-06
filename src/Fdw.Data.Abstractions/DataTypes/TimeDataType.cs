using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for time-of-day values (no date component).
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>time</c>, PostgreSQL <c>time without time zone</c>,
/// C# <c>System.TimeOnly</c> (.NET 6+) or <c>System.TimeSpan</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Time")]
public sealed class TimeDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeDataType"/> class.</summary>
    public TimeDataType()
        : base(id: 12, name: "Time")
    {
    }
}
