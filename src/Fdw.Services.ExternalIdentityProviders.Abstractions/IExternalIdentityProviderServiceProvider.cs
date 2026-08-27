using Fdw.ServiceTypes;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Resolves external identity providers by configuration name or id.
/// </summary>
public interface IExternalIdentityProviderServiceProvider
    : IPlatformServiceProvider<IExternalIdentityProvider, IExternalIdentityProviderImplementationConfiguration>
{
}
