using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>long</c>: 8 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Long")]
public sealed class LongRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="LongRowValueShape"/> class.</summary>
    public LongRowValueShape()
        : base("Long", typeof(long), 8)
    {
    }
}
