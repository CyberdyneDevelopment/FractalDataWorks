using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Pipelines.Loads;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for a pipeline's declared run parameters.
/// Targets the pipe.PipelineRunParameter table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade resolves the child command for a
/// <see cref="PipelineRunParameterConfiguration"/> item via <c>ConfigurationCommands.All()</c>; without this TypeOption
/// reading or saving a pipeline's run parameters fails loud (NoChildCommandForType) and the pipeline cannot load
/// (FDW-1130).
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineRunParameter")]
public sealed class PipelineRunParameterConfigurationCommand : ConfigurationCommandBase<PipelineRunParameterConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineRunParameter table.</summary>
    public PipelineRunParameterConfigurationCommand() : base("PipelineRunParameter") { }
}
