using Fdw.ServiceTypes;

using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Resolves external identity provisioners by configuration name or id.
/// </summary>
/// <remarks>
/// The domain's name for the platform contract, rather than a bare
/// <c>IPlatformServiceProvider&lt;IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration&gt;</c>
/// at every injection site. Narrowing at the configuration arity keeps the registration and
/// typed-configuration overloads reachable through it — an arity-1 interface hides them even though the
/// concrete provider implements them.
/// </remarks>
public interface IExternalIdentityProvisionerServiceProvider
    : IPlatformServiceProvider<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>
{
}
