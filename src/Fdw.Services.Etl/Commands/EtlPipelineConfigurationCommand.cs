using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Commands;

/// <summary>ConfigurationCommands TypeOption for the EtlPipeline configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "EtlPipeline")]
public sealed class EtlPipelineConfigurationCommand : ConfigurationCommandBase<EtlPipelineConfiguration>
{
    /// <summary>Initializes the command targeting the pipe.EtlPipeline table (the ETL-kind typed body).</summary>
    public EtlPipelineConfigurationCommand() : base("EtlPipeline") { }
}
