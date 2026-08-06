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
        : base(id: 11, name: "Date")
    {
    }
}
