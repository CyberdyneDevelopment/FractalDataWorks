using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// Supplies outgoing HTTP calls with a token proving this service's own identity, by bridging
/// <see cref="IIdentityService"/> onto the <see cref="IAccessTokenProvider"/> seam that
/// <c>BearerTokenHandler</c> already calls for every FDW typed client.
/// </summary>
/// <remarks>
/// <para>
/// Registering this is the whole integration: no call site changes, because every typed client
/// already goes through the seam.
/// </para>
/// <para>
/// <b>This does not replace user-token forwarding.</b> <c>BlazorServerAccessTokenProvider</c> and
/// <c>InstanceAccessTokenProvider</c> forward the signed-in user's token on purpose, so downstream
/// authorization runs as the real user. Substituting a service identity there would widen authority
/// rather than narrow it. Register this one only on clients whose calls have no user in the loop —
/// scheduled dispatches, background reconciliation, CI-initiated work.
/// </para>
/// <para>
/// The seam returns <c>Task&lt;string?&gt;</c>, so a failure cannot carry its reason across it. Every
/// failure is therefore logged here with the reason intact before the null is returned; the reason
/// survives in the log even though the signature cannot express it. Reshaping the seam to return
/// <c>IGenericResult</c> is deliberately out of scope — it is shared with in-flight work.
/// </para>
/// </remarks>
public sealed class ManagedIdentityAccessTokenProvider : IAccessTokenProvider
{
    private readonly IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration> _identities;
    private readonly IIdentityTokenCache _cache;
    private readonly ILogger<ManagedIdentityAccessTokenProvider> _logger;
    private readonly string _configurationName;
    private readonly IdentityTokenRequest _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedIdentityAccessTokenProvider"/> class.
    /// </summary>
    /// <param name="identities">Provider resolving the identity service by configuration name.</param>
    /// <param name="cache">The token cache shared across this process.</param>
    /// <param name="configurationName">The identity configuration this client authenticates as.</param>
    /// <param name="request">The audience and scopes this client needs a token for.</param>
    /// <param name="logger">The logger for this bridge.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="identities"/>, <paramref name="cache"/>, or <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configurationName"/> is null, empty, or whitespace.</exception>
    public ManagedIdentityAccessTokenProvider(
        IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration> identities,
        IIdentityTokenCache cache,
        string configurationName,
        IdentityTokenRequest request,
        ILogger<ManagedIdentityAccessTokenProvider>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(configurationName))
            throw new ArgumentException("A managed identity token provider must name the identity configuration it authenticates as.", nameof(configurationName));

        _identities = identities ?? throw new ArgumentNullException(nameof(identities));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _configurationName = configurationName;
        _logger = logger ?? NullLogger<ManagedIdentityAccessTokenProvider>.Instance;
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        var acquired = await _cache.GetOrAcquire(
            _configurationName,
            _request,
            async ct =>
            {
                var service = await _identities.Get(_configurationName, ct).ConfigureAwait(false);

                // Why the failure is propagated rather than relabelled: Get fails for reasons that are
                // not absence — a typed body that did not load, a factory that rejected the mechanism —
                // and each carries the reason that says which. Reporting them all as "no identity named
                // X exists" sends the reader to look for a missing row when the row is there.
                if (service.IsFailure)
                {
                    return service.ToNewResult<IssuedIdentityToken>();
                }

                if (service.Value is not { } identity)
                {
                    return GenericResult<IssuedIdentityToken>.Failure(
                        IdentityLog.ConfigurationNotFound(_logger, _configurationName));
                }

                return await identity.Acquire(_request, ct).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        if (acquired.IsFailure || acquired.Value is not { } token)
            return null;

        IdentityLog.OutboundTokenAttached(_logger, _configurationName, _request.Audience);
        return token.Value;
    }

    /// <summary>
    /// Drops this client's cached token so the next call acquires a fresh one.
    /// </summary>
    /// <remarks>
    /// Call this when a peer rejects a token that had not reached its expiry — the provider may have
    /// revoked it, and the cache would otherwise keep serving a token known to fail until the clock
    /// catches up.
    /// </remarks>
    public void InvalidateCachedToken() => _cache.Invalidate(_configurationName, _request);
}
