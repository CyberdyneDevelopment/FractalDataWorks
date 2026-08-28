using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Settings.Configuration;

namespace Fdw.Services.Settings.Commands;

/// <summary>ConfigurationCommands TypeOption for the ServerSetting configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "ServerSetting")]
public sealed class ServerSettingConfigurationCommand : ConfigurationCommandBase<ServerSettingConfiguration>
{
    /// <inheritdoc/>
    public ServerSettingConfigurationCommand() : base("ServerSetting") { }

    /// <inheritdoc/>
    protected override string NameColumn => "SettingName";
}
