using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>
/// Reads the permissions this platform defines.
/// </summary>
public interface IPermissionConfigurationProvider : IServiceConfigurationProvider<PermissionConfiguration>
{
}
