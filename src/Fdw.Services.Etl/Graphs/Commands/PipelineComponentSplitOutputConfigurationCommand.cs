using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the filtered outputs of a split. Targets the pipe.PipelineComponentSplitOutput table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineComponentSplitOutputConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponentSplitOutput")]
public sealed class PipelineComponentSplitOutputConfigurationCommand : ConfigurationCommandBase<PipelineComponentSplitOutputConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponentSplitOutput table.</summary>
    public PipelineComponentSplitOutputConfigurationCommand() : base("PipelineComponentSplitOutput") { }
}
