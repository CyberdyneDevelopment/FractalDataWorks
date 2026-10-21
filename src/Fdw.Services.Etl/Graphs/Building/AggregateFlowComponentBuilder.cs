using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds the Aggregate tool.</summary>
[TypeOption(typeof(FlowComponentBuilders), "Aggregate")]
public sealed class AggregateFlowComponentBuilder : StageToolFlowComponentBuilder
{
    /// <summary>Initializes a new instance of the <see cref="AggregateFlowComponentBuilder"/> class.</summary>
    public AggregateFlowComponentBuilder() : base("Aggregate")
    {
    }
}
