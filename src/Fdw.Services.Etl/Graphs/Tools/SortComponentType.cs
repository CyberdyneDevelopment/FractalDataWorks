using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Tools;

/// <summary>The Sort tool: the Sort stage kind placed in a graph.</summary>
[TypeOption(typeof(ComponentTypes), "Sort")]
public sealed class SortComponentType : StageToolComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SortComponentType"/> class.</summary>
    public SortComponentType() : base(new SortTransformType(), "Transform", "The same rows, in the order the fields say.")
    {
    }
}
