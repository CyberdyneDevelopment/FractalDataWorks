using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes.Logging;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Fdw.Services.Users;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// Builds the ClaimMappedProvisioner implementation.
/// </summary>
/// <remarks>
/// Nothing to resolve here, so it completes synchronously. The provider contract is asynchronous
/// because RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class ClaimMappedProvisionerProvider
    : ImplementationServiceProviderBase<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>,
      IClaimMappedProvisionerProvider
{
    private readonly IClaimMappedProvisionerFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimMappedProvisionerProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the service.</param>
    public ClaimMappedProvisionerProvider(IClaimMappedProvisionerFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IExternalIdentityProvisioner>> Create(
        IExternalIdentityProvisionerImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration));
}
