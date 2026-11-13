using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetStageComputeScalarConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetStageComputeScalarConfiguration")]
public sealed class GraphDataSetStageComputeScalarConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetStageComputeScalarConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageComputeScalarConfigurationNoValidation"/> class.</summary>
    public GraphDataSetStageComputeScalarConfigurationNoValidation()
        : base("GraphDataSetStageComputeScalarConfiguration", "A stage's content is checked against its tool's own rules when the graph is saved (story FDW-5290); until that check states its rules here this says so. Replace this with a validator when its rules are stated.")
    {
    }
}
