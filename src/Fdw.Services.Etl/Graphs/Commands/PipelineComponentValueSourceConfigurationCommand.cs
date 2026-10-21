using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the value sources of a destination component. Targets the pipe.PipelineComponentValueSource table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineComponentValueSourceConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponentValueSource")]
public sealed class PipelineComponentValueSourceConfigurationCommand : ConfigurationCommandBase<PipelineComponentValueSourceConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponentValueSource table.</summary>
    public PipelineComponentValueSourceConfigurationCommand() : base("PipelineComponentValueSource") { }
}
