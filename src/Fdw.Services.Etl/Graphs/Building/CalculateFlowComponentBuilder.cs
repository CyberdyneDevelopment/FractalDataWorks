using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds the Calculate tool.</summary>
[TypeOption(typeof(FlowComponentBuilders), "Calculate")]
public sealed class CalculateFlowComponentBuilder : StageToolFlowComponentBuilder
{
    /// <summary>Initializes a new instance of the <see cref="CalculateFlowComponentBuilder"/> class.</summary>
    public CalculateFlowComponentBuilder() : base("Calculate")
    {
    }
}
