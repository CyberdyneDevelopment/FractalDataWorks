using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds the Distinct tool.</summary>
[TypeOption(typeof(FlowComponentBuilders), "Distinct")]
public sealed class DistinctFlowComponentBuilder : StageToolFlowComponentBuilder
{
    /// <summary>Initializes a new instance of the <see cref="DistinctFlowComponentBuilder"/> class.</summary>
    public DistinctFlowComponentBuilder() : base("Distinct")
    {
    }
}
