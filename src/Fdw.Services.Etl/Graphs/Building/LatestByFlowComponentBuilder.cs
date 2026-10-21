using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds the LatestBy tool.</summary>
[TypeOption(typeof(FlowComponentBuilders), "LatestBy")]
public sealed class LatestByFlowComponentBuilder : StageToolFlowComponentBuilder
{
    /// <summary>Initializes a new instance of the <see cref="LatestByFlowComponentBuilder"/> class.</summary>
    public LatestByFlowComponentBuilder() : base("LatestBy")
    {
    }
}
