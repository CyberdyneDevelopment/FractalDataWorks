using Fdw.Services.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>Supplies the configured ServerSetting members.</summary>
public interface IServerSettingConfigurationProvider
    : IDomainConfigurationProvider<IServerSettingImplementationConfiguration>
{
}
