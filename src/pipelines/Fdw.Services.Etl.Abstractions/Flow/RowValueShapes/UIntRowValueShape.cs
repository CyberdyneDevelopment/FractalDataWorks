using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>uint</c>: 4 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "UInt")]
public sealed class UIntRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="UIntRowValueShape"/> class.</summary>
    public UIntRowValueShape()
        : base("UInt", typeof(uint), 4)
    {
    }
}
