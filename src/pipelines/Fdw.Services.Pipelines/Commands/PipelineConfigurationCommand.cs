using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Pipelines.Commands;

/// <summary>ConfigurationCommands TypeOption for the Pipeline configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Pipeline")]
public sealed class PipelineConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineConfigurationCommand"/> class.</summary>
    public PipelineConfigurationCommand() : base("Pipeline") { }
}
