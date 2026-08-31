using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>
/// Reads the role-to-permission grants.
/// </summary>
public interface IRolePermissionConfigurationProvider : IServiceConfigurationProvider<RolePermissionConfiguration>
{
}
