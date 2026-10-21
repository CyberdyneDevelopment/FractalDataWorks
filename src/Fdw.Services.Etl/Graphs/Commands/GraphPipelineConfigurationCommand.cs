using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the implementation row of a Graph pipeline. Targets the pipe.GraphPipelineConfiguration table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="GraphPipelineConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "GraphPipelineConfiguration")]
public sealed class GraphPipelineConfigurationCommand : ConfigurationCommandBase<GraphPipelineConfiguration>
{
    /// <summary>Initializes the command targeting the GraphPipelineConfiguration table.</summary>
    public GraphPipelineConfigurationCommand() : base("GraphPipelineConfiguration") { }
}
