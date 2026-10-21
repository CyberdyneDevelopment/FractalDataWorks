using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the variables of a graph. Targets the pipe.PipelineVariable table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineVariableConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineVariable")]
public sealed class PipelineVariableConfigurationCommand : ConfigurationCommandBase<PipelineVariableConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineVariable table.</summary>
    public PipelineVariableConfigurationCommand() : base("PipelineVariable") { }
}
