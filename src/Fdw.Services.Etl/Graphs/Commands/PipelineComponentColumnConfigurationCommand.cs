using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the column lists of a component. Targets the pipe.PipelineComponentColumn table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineComponentColumnConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponentColumn")]
public sealed class PipelineComponentColumnConfigurationCommand : ConfigurationCommandBase<PipelineComponentColumnConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponentColumn table.</summary>
    public PipelineComponentColumnConfigurationCommand() : base("PipelineComponentColumn") { }
}
