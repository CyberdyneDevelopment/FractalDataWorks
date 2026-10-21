using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the dropped outputs of a graph. Targets the pipe.PipelineDroppedOutput table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineDroppedOutputConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineDroppedOutput")]
public sealed class PipelineDroppedOutputConfigurationCommand : ConfigurationCommandBase<PipelineDroppedOutputConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineDroppedOutput table.</summary>
    public PipelineDroppedOutputConfigurationCommand() : base("PipelineDroppedOutput") { }
}
