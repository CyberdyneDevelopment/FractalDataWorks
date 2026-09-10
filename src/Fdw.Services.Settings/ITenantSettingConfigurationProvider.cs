using Fdw.Services.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>Supplies the configured TenantSetting members.</summary>
public interface ITenantSettingConfigurationProvider
    : IDomainConfigurationProvider<ITenantSettingImplementationConfiguration>
{
}
