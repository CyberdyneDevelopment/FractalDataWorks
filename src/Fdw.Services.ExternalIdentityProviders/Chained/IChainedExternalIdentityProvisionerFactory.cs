using System;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// The factory for the chained external identity provisioner.
/// </summary>
/// <remarks>
/// Its own type, so the container can tell this implementation's factory from every other one in
/// the domain. Registering them all under the domain's own closed generic gave each implementation
/// the same service type, and TryAdd keeps the first -- so whichever option initialised first was
/// the only one that could ever be resolved.
/// </remarks>
public interface IChainedExternalIdentityProvisionerFactory : IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>
{
}
