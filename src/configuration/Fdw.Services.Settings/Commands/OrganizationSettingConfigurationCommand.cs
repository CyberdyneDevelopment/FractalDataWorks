using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Settings.Configuration;

namespace Fdw.Services.Settings.Commands;

/// <summary>ConfigurationCommands TypeOption for the OrganizationSetting configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "OrganizationSetting")]
public sealed class OrganizationSettingConfigurationCommand : ConfigurationCommandBase<OrganizationSettingImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OrganizationSettingConfigurationCommand"/> class.</summary>
    public OrganizationSettingConfigurationCommand() : base("OrganizationSetting") { }
}
