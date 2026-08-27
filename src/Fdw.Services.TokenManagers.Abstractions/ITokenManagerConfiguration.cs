using Fdw.Configuration;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// One configured token manager — the domain record, naming which implementation it is and holding
/// that implementation's own configuration.
/// </summary>
public interface ITokenManagerConfiguration
    : IPlatformServiceConfiguration<ITokenManagerImplementationConfiguration>
{
}
