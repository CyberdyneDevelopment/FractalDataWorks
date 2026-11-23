using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the declared settings of a component (FDW-6133). Targets the pipe.PipelineComponentSetting table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineComponentSettingConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponentSetting")]
public sealed class PipelineComponentSettingConfigurationCommand : ConfigurationCommandBase<PipelineComponentSettingConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponentSetting table.</summary>
    public PipelineComponentSettingConfigurationCommand() : base("PipelineComponentSetting") { }
}
