using Fdw.Services.Settings.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>Supplies the configured RoleSetting members.</summary>
public interface IRoleSettingConfigurationProvider
    : IDomainConfigurationProvider<IRoleSettingImplementationConfiguration>
{
}
