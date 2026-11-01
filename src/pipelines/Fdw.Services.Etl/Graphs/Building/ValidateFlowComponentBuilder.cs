using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds the Validate tool.</summary>
[TypeOption(typeof(FlowComponentBuilders), "Validate")]
public sealed class ValidateFlowComponentBuilder : StageToolFlowComponentBuilder
{
    /// <summary>Initializes a new instance of the <see cref="ValidateFlowComponentBuilder"/> class.</summary>
    public ValidateFlowComponentBuilder() : base("Validate")
    {
    }
}
