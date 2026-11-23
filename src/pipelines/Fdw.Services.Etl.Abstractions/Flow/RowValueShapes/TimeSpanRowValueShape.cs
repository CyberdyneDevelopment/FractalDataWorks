using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>System.TimeSpan</c>: 8 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "TimeSpan")]
public sealed class TimeSpanRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeSpanRowValueShape"/> class.</summary>
    public TimeSpanRowValueShape()
        : base("TimeSpan", typeof(System.TimeSpan), 8)
    {
    }
}
