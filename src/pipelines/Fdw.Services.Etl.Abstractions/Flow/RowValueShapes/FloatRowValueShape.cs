using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>float</c>: 4 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Float")]
public sealed class FloatRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatRowValueShape"/> class.</summary>
    public FloatRowValueShape()
        : base("Float", typeof(float), 4)
    {
    }
}
