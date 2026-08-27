using Fdw.ServiceTypes;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Resolves token managers by configuration name or id.
/// </summary>
public interface ITokenManagerProvider
    : IPlatformServiceProvider<ITokenManager, ITokenManagerImplementationConfiguration>
{
}
