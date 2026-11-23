using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>double</c>: 8 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Double")]
public sealed class DoubleRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DoubleRowValueShape"/> class.</summary>
    public DoubleRowValueShape()
        : base("Double", typeof(double), 8)
    {
    }
}
