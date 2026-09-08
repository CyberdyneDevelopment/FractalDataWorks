using Fdw.Services.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Resolves configured external identity provisioners and routes each to the implementation provider
/// that owns it.
/// </summary>
public interface IExternalIdentityProvisionerConfigurationProvider
    : IDomainConfigurationProvider<IExternalIdentityProvisionerImplementationConfiguration>
{
}
