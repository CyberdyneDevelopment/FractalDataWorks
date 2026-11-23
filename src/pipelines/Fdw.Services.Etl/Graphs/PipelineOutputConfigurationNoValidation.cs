using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Graphs;

/// <summary>States that <see cref="PipelineOutputConfiguration"/> has no validator of its own, and why (FDW-6136).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineOutputConfiguration")]
public sealed class PipelineOutputConfigurationNoValidation : UnvalidatedConfiguration<PipelineOutputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineOutputConfigurationNoValidation"/> class.</summary>
    public PipelineOutputConfigurationNoValidation()
        : base("PipelineOutputConfiguration", "The graph's save judges its outputs together with its variables (GraphComponentValidator), because an output is right or wrong only against the graph's variables; this row alone has nothing to judge.")
    {
    }
}
