using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the components of a graph. Targets the pipe.PipelineComponent table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineComponentConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponent")]
public sealed class PipelineComponentConfigurationCommand : ConfigurationCommandBase<PipelineComponentConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponent table.</summary>
    public PipelineComponentConfigurationCommand() : base("PipelineComponent") { }
}
