using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>int</c>: 4 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Int")]
public sealed class IntRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="IntRowValueShape"/> class.</summary>
    public IntRowValueShape()
        : base("Int", typeof(int), 4)
    {
    }
}
