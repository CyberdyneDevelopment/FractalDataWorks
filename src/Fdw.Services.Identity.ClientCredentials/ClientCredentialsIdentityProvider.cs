using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.Services.Identity;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>
/// Builds the ClientCredentialsIdentity implementation.
/// </summary>
/// <remarks>
/// Nothing to resolve here, so it completes synchronously. The provider contract is asynchronous
/// because RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class ClientCredentialsIdentityProvider
    : ImplementationServiceProviderBase<IIdentityService, IIdentityServiceImplementationConfiguration>,
      IClientCredentialsIdentityProvider
{
    private readonly IClientCredentialsIdentityFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientCredentialsIdentityProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the service.</param>
    public ClientCredentialsIdentityProvider(IClientCredentialsIdentityFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IIdentityService>> Create(
        IIdentityServiceImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration));
}
