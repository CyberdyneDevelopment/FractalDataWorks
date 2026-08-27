using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// One configured external identity provisioner — the domain record, naming which implementation it is
/// and holding that implementation's own configuration.
/// </summary>
public interface IExternalIdentityProvisionerConfiguration
    : IPlatformServiceConfiguration<IExternalIdentityProvisionerImplementationConfiguration>
{
}
