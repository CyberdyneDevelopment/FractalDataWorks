using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.Services.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>
/// Builds the JwtAssertionIdentity implementation.
/// </summary>
/// <remarks>
/// Nothing to resolve here, so it completes synchronously. The provider contract is asynchronous
/// because RESOLVING may be; an implementation with nothing to fetch has nothing to await.
/// </remarks>
public sealed class JwtAssertionIdentityProvider
    : ImplementationServiceProviderBase<IIdentityService, IIdentityServiceImplementationConfiguration>,
      IJwtAssertionIdentityProvider
{
    private readonly IJwtAssertionIdentityFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtAssertionIdentityProvider"/> class.
    /// </summary>
    /// <param name="factory">Builds the service.</param>
    public JwtAssertionIdentityProvider(IJwtAssertionIdentityFactory factory) => _factory = factory;

    /// <inheritdoc />
    public override Task<IGenericResult<IIdentityService>> Create(
        IIdentityServiceImplementationConfiguration configuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_factory.Create(configuration));
}
