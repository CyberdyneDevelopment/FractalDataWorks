using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the links of a graph. Targets the pipe.PipelineLink table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineLinkConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineLink")]
public sealed class PipelineLinkConfigurationCommand : ConfigurationCommandBase<PipelineLinkConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineLink table.</summary>
    public PipelineLinkConfigurationCommand() : base("PipelineLink") { }
}
