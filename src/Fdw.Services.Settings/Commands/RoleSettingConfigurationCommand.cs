using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Settings.Configuration;

namespace Fdw.Services.Settings.Commands;

/// <summary>ConfigurationCommands TypeOption for the RoleSetting configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "RoleSetting")]
public sealed class RoleSettingConfigurationCommand : ConfigurationCommandBase<RoleSettingConfiguration>
{
    /// <inheritdoc/>
    public RoleSettingConfigurationCommand() : base("RoleSetting") { }
}
