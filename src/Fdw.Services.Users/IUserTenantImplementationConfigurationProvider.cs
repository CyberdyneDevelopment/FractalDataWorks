using Fdw.Services.Abstractions;
using Fdw.Services.Users.Configuration;

namespace Fdw.Services.Users;

/// <summary>Supplies the UserTenants implementation's own configuration to the domain that registers it.</summary>
public interface IUserTenantImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IUserTenantImplementationConfiguration>
{
}
