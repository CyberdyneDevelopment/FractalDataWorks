using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetStageComputeInputConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetStageComputeInputConfiguration")]
public sealed class GraphDataSetStageComputeInputConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetStageComputeInputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetStageComputeInputConfigurationNoValidation"/> class.</summary>
    public GraphDataSetStageComputeInputConfigurationNoValidation()
        : base("GraphDataSetStageComputeInputConfiguration", "A stage's content is checked against its tool's own rules when the graph is saved (story FDW-5290); until that check states its rules here this says so. Replace this with a validator when its rules are stated.")
    {
    }
}
