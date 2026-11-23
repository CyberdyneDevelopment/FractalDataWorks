using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Graphs;

/// <summary>States that <see cref="GraphPipelineConfiguration"/> is checked by <see cref="GraphPipelineConfigurationValidator"/> when it is written (FDW-6141).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphPipelineConfiguration")]
public sealed class GraphPipelineConfigurationValidation : ValidatedConfiguration<GraphPipelineConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphPipelineConfigurationValidation"/> class.</summary>
    public GraphPipelineConfigurationValidation()
        : base("GraphPipelineConfiguration", new GraphPipelineConfigurationValidator())
    {
    }
}
