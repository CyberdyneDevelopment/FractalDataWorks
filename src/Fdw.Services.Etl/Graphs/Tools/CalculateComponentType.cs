using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Tools;

/// <summary>The Calculate tool: the Calculate stage kind placed in a graph.</summary>
[TypeOption(typeof(ComponentTypes), "Calculate")]
public sealed class CalculateComponentType : StageToolComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CalculateComponentType"/> class.</summary>
    public CalculateComponentType() : base(new CalculateTransformType(), "Calculate", "Every row, with the calculated fields added.")
    {
    }
}
