using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>decimal</c>: 16 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Decimal")]
public sealed class DecimalRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalRowValueShape"/> class.</summary>
    public DecimalRowValueShape()
        : base("Decimal", typeof(decimal), 16)
    {
    }
}
