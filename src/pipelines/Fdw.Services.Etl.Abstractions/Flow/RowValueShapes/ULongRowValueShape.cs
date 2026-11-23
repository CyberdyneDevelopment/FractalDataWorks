using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>ulong</c>: 8 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "ULong")]
public sealed class ULongRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="ULongRowValueShape"/> class.</summary>
    public ULongRowValueShape()
        : base("ULong", typeof(ulong), 8)
    {
    }
}
