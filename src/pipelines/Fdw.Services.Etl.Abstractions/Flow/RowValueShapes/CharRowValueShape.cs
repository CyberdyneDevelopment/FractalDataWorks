using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>char</c>: 2 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Char")]
public sealed class CharRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="CharRowValueShape"/> class.</summary>
    public CharRowValueShape()
        : base("Char", typeof(char), 2)
    {
    }
}
