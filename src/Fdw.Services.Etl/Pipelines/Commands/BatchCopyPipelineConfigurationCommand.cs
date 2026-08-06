using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Pipelines.Commands;

/// <summary>ConfigurationCommands TypeOption for the BatchCopyPipeline configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "BatchCopyPipeline")]
public sealed class BatchCopyPipelineConfigurationCommand : ConfigurationCommandBase<BatchCopyPipelineConfiguration>
{
    /// <inheritdoc/>
    public BatchCopyPipelineConfigurationCommand() : base("BatchCopyPipeline") { }
}
