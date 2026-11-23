using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>sbyte</c>: 1 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "SByte")]
public sealed class SByteRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="SByteRowValueShape"/> class.</summary>
    public SByteRowValueShape()
        : base("SByte", typeof(sbyte), 1)
    {
    }
}
