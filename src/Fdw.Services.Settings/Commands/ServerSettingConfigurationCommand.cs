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

    // Why: settings.ServerSetting keys on SettingName, not Name — the default "Name" column does not
    // exist there (SQL 207). Get(name)/Update filter by SettingName so the setting loads and saves.
    /// <inheritdoc/>
    protected override string NameColumn => "SettingName";
}
