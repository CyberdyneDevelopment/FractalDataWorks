using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Tools;

/// <summary>The Distinct tool: the Distinct stage kind placed in a graph.</summary>
[TypeOption(typeof(ComponentTypes), "Distinct")]
public sealed class DistinctComponentType : StageToolComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DistinctComponentType"/> class.</summary>
    public DistinctComponentType() : base(new DistinctTransformType(), "Filter", "The first row of each key; the repeats are dropped.")
    {
    }
}
