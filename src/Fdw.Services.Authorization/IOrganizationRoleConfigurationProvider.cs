using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the roles organizations define for their own members (FDW-1593).</summary>
public interface IOrganizationRoleConfigurationProvider
    : IDomainConfigurationProvider<IOrganizationRoleImplementationConfiguration>
{
}
