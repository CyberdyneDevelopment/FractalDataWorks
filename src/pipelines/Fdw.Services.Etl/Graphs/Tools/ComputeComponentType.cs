using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Tools;

/// <summary>The Compute tool: a Built transformation applied to every row, adding its results (FDW-1615).</summary>
[TypeOption(typeof(ComponentTypes), "Compute")]
public sealed class ComputeComponentType : StageToolComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ComputeComponentType"/> class.</summary>
    public ComputeComponentType() : base(new ComputeTransformType(), "Calculate", "Every row, with the results of the transformation added.", mayHoldEveryRow: true)
    {
    }
}
