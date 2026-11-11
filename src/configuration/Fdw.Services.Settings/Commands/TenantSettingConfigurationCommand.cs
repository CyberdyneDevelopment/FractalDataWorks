using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Settings.Configuration;

namespace Fdw.Services.Settings.Commands;

/// <summary>ConfigurationCommands TypeOption for the TenantSetting configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "TenantSetting")]
public sealed class TenantSettingConfigurationCommand : ConfigurationCommandBase<TenantSettingImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TenantSettingConfigurationCommand"/> class.</summary>
    public TenantSettingConfigurationCommand() : base("TenantSetting") { }
}
