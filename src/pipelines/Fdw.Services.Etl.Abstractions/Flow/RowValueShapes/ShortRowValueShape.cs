using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>short</c>: 2 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Short")]
public sealed class ShortRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="ShortRowValueShape"/> class.</summary>
    public ShortRowValueShape()
        : base("Short", typeof(short), 2)
    {
    }
}
