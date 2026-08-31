using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>
/// Reads the roles this platform defines.
/// </summary>
/// <remarks>
/// Named rather than consumed as a bare <c>IServiceConfigurationProvider&lt;RoleConfiguration&gt;</c>:
/// a consumer asking for the closed generic states a shape, this states which rows it reads, and two
/// providers over different tables are no longer interchangeable at a constructor.
/// </remarks>
public interface IRoleConfigurationProvider : IServiceConfigurationProvider<RoleConfiguration>
{
}
