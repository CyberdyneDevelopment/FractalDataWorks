using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>System.DateTimeOffset</c>: 16 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "DateTimeOffset")]
public sealed class DateTimeOffsetRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetRowValueShape"/> class.</summary>
    public DateTimeOffsetRowValueShape()
        : base("DateTimeOffset", typeof(System.DateTimeOffset), 16)
    {
    }
}
