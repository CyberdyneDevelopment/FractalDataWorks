using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the ordering columns of a value source. Targets the pipe.PipelineComponentValueSourceColumn table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineComponentValueSourceColumnConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponentValueSourceColumn")]
public sealed class PipelineComponentValueSourceColumnConfigurationCommand : ConfigurationCommandBase<PipelineComponentValueSourceColumnConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponentValueSourceColumn table.</summary>
    public PipelineComponentValueSourceColumnConfigurationCommand() : base("PipelineComponentValueSourceColumn") { }
}
