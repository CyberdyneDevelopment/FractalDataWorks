using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// Builds the ChainedExternalIdentityProvisioner implementation.
/// </summary>
/// <remarks>
/// Nothing to resolve here, so it completes synchronously. The provider contract is asynchronous
/// because RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class ChainedExternalIdentityProvisionerProvider
    : ImplementationServiceProviderBase<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>,
      IChainedExternalIdentityProvisionerProvider
{
    private readonly IChainedExternalIdentityProvisionerFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainedExternalIdentityProvisionerProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the service.</param>
    public ChainedExternalIdentityProvisionerProvider(IChainedExternalIdentityProvisionerFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IExternalIdentityProvisioner>> Create(
        IExternalIdentityProvisionerImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration));
}
