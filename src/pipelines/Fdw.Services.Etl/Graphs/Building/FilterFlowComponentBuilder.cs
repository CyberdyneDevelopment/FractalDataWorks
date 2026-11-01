using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds the Filter tool.</summary>
[TypeOption(typeof(FlowComponentBuilders), "Filter")]
public sealed class FilterFlowComponentBuilder : StageToolFlowComponentBuilder
{
    /// <summary>Initializes a new instance of the <see cref="FilterFlowComponentBuilder"/> class.</summary>
    public FilterFlowComponentBuilder() : base("Filter")
    {
    }
}
