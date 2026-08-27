using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// One configured external identity provider — the domain record, naming which implementation it is
/// and holding that implementation's own configuration.
/// </summary>
public interface IExternalIdentityProviderConfiguration
    : IPlatformServiceConfiguration<IExternalIdentityProviderImplementationConfiguration>
{
}
