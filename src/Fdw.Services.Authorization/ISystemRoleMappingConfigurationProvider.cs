using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the SystemRoleMapping implementation's own configuration.</summary>
public interface ISystemRoleMappingConfigurationProvider
    : IImplementationConfigurationProvider<SystemRoleMappingConfiguration>
{
}
