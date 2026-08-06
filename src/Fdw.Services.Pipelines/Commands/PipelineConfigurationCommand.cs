using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Pipelines.Commands;

/// <summary>ConfigurationCommands TypeOption for the Pipeline configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Pipeline")]
public sealed class PipelineConfigurationCommand : ConfigurationCommandBase<PipelineConfiguration>
{
    /// <inheritdoc/>
    public PipelineConfigurationCommand() : base("Pipeline") { }
}
