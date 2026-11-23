using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>byte</c>: 1 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Byte")]
public sealed class ByteRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="ByteRowValueShape"/> class.</summary>
    public ByteRowValueShape()
        : base("Byte", typeof(byte), 1)
    {
    }
}
