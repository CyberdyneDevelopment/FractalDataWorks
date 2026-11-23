using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>bool</c>: 1 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Bool")]
public sealed class BoolRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="BoolRowValueShape"/> class.</summary>
    public BoolRowValueShape()
        : base("Bool", typeof(bool), 1)
    {
    }
}
