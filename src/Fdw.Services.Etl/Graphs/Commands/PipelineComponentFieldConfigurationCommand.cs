using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the field mappings of a component. Targets the pipe.PipelineComponentField table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineComponentFieldConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponentField")]
public sealed class PipelineComponentFieldConfigurationCommand : ConfigurationCommandBase<PipelineComponentFieldConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponentField table.</summary>
    public PipelineComponentFieldConfigurationCommand() : base("PipelineComponentField") { }
}
