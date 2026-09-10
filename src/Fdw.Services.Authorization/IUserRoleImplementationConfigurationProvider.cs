using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the UserRole implementation's own configuration to the domain that registers it.</summary>
public interface IUserRoleImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IUserRoleImplementationConfiguration>
{
}
