using Fdw.Services.Abstractions;

namespace Fdw.Services.Users;

/// <summary>Supplies the configured UserTenants members.</summary>
public interface IUserTenantConfigurationProvider
    : IDomainConfigurationProvider<IUserTenantImplementationConfiguration>
{
}
