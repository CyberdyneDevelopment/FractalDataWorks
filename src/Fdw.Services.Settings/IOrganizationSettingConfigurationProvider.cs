using Fdw.Services.Settings.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>Supplies the configured OrganizationSetting members.</summary>
public interface IOrganizationSettingConfigurationProvider
    
    : IDomainConfigurationProvider<IOrganizationSettingImplementationConfiguration>
{
}
