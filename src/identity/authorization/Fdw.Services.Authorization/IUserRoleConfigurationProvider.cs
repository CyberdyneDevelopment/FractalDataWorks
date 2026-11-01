using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the configured UserRole members.</summary>
public interface IUserRoleConfigurationProvider
    : IDomainConfigurationProvider<IUserRoleImplementationConfiguration>
{
}
