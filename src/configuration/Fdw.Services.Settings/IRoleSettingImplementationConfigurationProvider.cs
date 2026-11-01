using Fdw.Services.Abstractions;
using Fdw.Services.Settings.Configuration;

namespace Fdw.Services.Settings;

/// <summary>Supplies the RoleSetting implementation's own configuration to the domain that registers it.</summary>
public interface IRoleSettingImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IRoleSettingImplementationConfiguration>
{
}
