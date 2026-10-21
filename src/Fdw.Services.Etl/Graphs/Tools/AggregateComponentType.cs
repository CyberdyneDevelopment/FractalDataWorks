using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Tools;

/// <summary>The Aggregate tool: the Aggregate stage kind placed in a graph.</summary>
[TypeOption(typeof(ComponentTypes), "Aggregate")]
public sealed class AggregateComponentType : StageToolComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AggregateComponentType"/> class.</summary>
    public AggregateComponentType() : base(new AggregateTransformType(), "Calculate", "One row per group, with the group fields and the aggregates.")
    {
    }
}
