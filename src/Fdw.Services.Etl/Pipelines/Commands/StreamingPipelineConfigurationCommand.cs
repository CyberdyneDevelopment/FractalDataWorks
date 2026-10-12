using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Pipelines.Commands;

/// <summary>ConfigurationCommands TypeOption for the StreamingPipeline configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "StreamingPipelineConfiguration")]
public sealed class StreamingPipelineConfigurationCommand : ConfigurationCommandBase<StreamingPipelineConfiguration>
{
    /// <inheritdoc/>
    public StreamingPipelineConfigurationCommand() : base("StreamingPipelineConfiguration") { }
}
