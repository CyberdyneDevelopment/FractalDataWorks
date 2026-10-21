using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the parameters of a component. Targets the pipe.PipelineComponentParameter table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineComponentParameterConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponentParameter")]
public sealed class PipelineComponentParameterConfigurationCommand : ConfigurationCommandBase<PipelineComponentParameterConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponentParameter table.</summary>
    public PipelineComponentParameterConfigurationCommand() : base("PipelineComponentParameter") { }
}
