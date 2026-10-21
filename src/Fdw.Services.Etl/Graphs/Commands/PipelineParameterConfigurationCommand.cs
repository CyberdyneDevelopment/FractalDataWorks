using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the parameters of a graph. Targets the pipe.PipelineParameter table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineParameterConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineParameter")]
public sealed class PipelineParameterConfigurationCommand : ConfigurationCommandBase<PipelineParameterConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PipelineParameterConfigurationCommand"/> class.</summary>
    public PipelineParameterConfigurationCommand() : base("PipelineParameter") { }
}
