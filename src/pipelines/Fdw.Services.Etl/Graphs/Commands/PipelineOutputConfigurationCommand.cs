using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the outputs of a graph (FDW-6136). Targets the pipe.PipelineOutput table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineOutputConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineOutput")]
public sealed class PipelineOutputConfigurationCommand : ConfigurationCommandBase<PipelineOutputConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineOutput table.</summary>
    public PipelineOutputConfigurationCommand() : base("PipelineOutput") { }
}
