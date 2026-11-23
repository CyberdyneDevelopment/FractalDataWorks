using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Etl.Graphs;

/// <summary>States that <see cref="PipelineComponentSettingConfiguration"/> has no validator of its own, and why (FDW-6133).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PipelineComponentSettingConfiguration")]
public sealed class PipelineComponentSettingConfigurationNoValidation : UnvalidatedConfiguration<PipelineComponentSettingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineComponentSettingConfigurationNoValidation"/> class.</summary>
    public PipelineComponentSettingConfigurationNoValidation()
        : base("PipelineComponentSettingConfiguration", "A declared setting is judged against its component's type when the graph is saved (ComponentSettings.Problems), which knows the declaration; the row alone cannot be.")
    {
    }
}
