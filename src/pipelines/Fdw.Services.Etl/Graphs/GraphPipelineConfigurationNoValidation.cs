using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Graphs;

/// <summary>States that <see cref="GraphPipelineConfiguration"/> has no validator, and why (FDW-3541).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphPipelineConfiguration")]
public sealed class GraphPipelineConfigurationNoValidation : UnvalidatedConfiguration<GraphPipelineConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphPipelineConfigurationNoValidation"/> class.</summary>
    public GraphPipelineConfigurationNoValidation()
        : base("GraphPipelineConfiguration", "No validator exists for this configuration yet; recorded by the FDW-3541 audit so that writing it is a stated decision. Replace this with a validator when its rules are stated.")
    {
    }
}
