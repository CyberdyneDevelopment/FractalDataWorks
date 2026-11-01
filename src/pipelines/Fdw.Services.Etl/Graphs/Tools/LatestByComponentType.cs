using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Tools;

/// <summary>The LatestBy tool: the LatestBy stage kind placed in a graph.</summary>
[TypeOption(typeof(ComponentTypes), "LatestBy")]
public sealed class LatestByComponentType : StageToolComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LatestByComponentType"/> class.</summary>
    public LatestByComponentType() : base(new LatestByTransformType(), "Filter", "One row per key: the first or last by the order field.")
    {
    }
}
