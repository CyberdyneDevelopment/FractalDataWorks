using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Graphs.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the setting bindings of components. Targets the pipe.PipelineComponentSettingBinding table.
/// </summary>
/// <remarks>
/// Why: the configuration cascade-save resolves the command for a <see cref="PipelineComponentSettingBindingConfiguration"/> item via
/// <c>ConfigurationCommands.All()</c>; without this TypeOption the cascade would fail loud when persisting a graph.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "PipelineComponentSettingBinding")]
public sealed class PipelineComponentSettingBindingConfigurationCommand : ConfigurationCommandBase<PipelineComponentSettingBindingConfiguration>
{
    /// <summary>Initializes the command targeting the PipelineComponentSettingBinding table.</summary>
    public PipelineComponentSettingBindingConfigurationCommand() : base("PipelineComponentSettingBinding") { }
}
