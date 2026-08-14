using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// In-memory, expiry-aware cache of issued identity tokens.
/// </summary>
/// <remarks>
/// <para>
/// Tokens are held in memory only and never persisted. A token is a bearer credential; writing one
/// anywhere durable creates a longer-lived secret than the static one this domain exists to remove.
/// </para>
/// <para>
/// Acquisition is serialized per cache key so that a burst of outbound calls arriving with no live
/// token results in one acquisition rather than one per call. Without this the identity provider
/// sees a thundering herd at exactly the moment a token ages out.
/// </para>
/// </remarks>
public sealed class IdentityTokenCache : IIdentityTokenCache
{
    /// <summary>
    /// How long before actual expiry a token stops being served from cache.
    /// </summary>
    /// <remarks>
    /// Why a constant of the mechanism rather than a configuration value: this is headroom for the
    /// in-flight request, not a policy anyone tunes per identity. It must exceed the round trip to
    /// the peer, and 60 seconds covers that with room for clock skew between FDW and the issuer.
    /// It is passed explicitly at the single construction site so nothing is silently assumed.
    /// </remarks>
    public static readonly TimeSpan DefaultRefreshSkew = TimeSpan.FromSeconds(60);

    private readonly ILogger<IdentityTokenCache> _logger;
    private readonly TimeSpan _refreshSkew;
    private readonly ConcurrentDictionary<string, IssuedIdentityToken> _tokens = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="IdentityTokenCache"/> class.</summary>
    /// <param name="logger">The logger for this cache.</param>
    /// <param name="refreshSkew">How long before actual expiry a token stops being served.</param>
    public IdentityTokenCache(ILogger<IdentityTokenCache> logger, TimeSpan refreshSkew)
    {
        _logger = logger ?? NullLogger<IdentityTokenCache>.Instance;
        _refreshSkew = refreshSkew;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IssuedIdentityToken>> GetOrAcquire(
        string configurationName,
        IdentityTokenRequest request,
        Func<CancellationToken, Task<IGenericResult<IssuedIdentityToken>>> acquire,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(_logger, configurationName, nameof(request)));
        if (acquire is null)
            return GenericResult<IssuedIdentityToken>.Failure(IdentityLog.ConfigurationValueMissing(_logger, configurationName, nameof(acquire)));

        if (TryServe(configurationName, request, out var cached))
            return GenericResult<IssuedIdentityToken>.Success(cached);

        IdentityLog.TokenCacheMiss(_logger, configurationName, request.Audience);

        var gate = _gates.GetOrAdd(KeyFor(configurationName, request), _ => new SemaphoreSlim(1, 1));
        if (gate.CurrentCount == 0)
            IdentityLog.AwaitingInFlightAcquisition(_logger, configurationName, request.Audience);

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Why the second check: another caller may have acquired while this one waited on the
            // gate, and re-acquiring would defeat the serialization the gate exists to provide.
            if (TryServe(configurationName, request, out var justAcquired))
                return GenericResult<IssuedIdentityToken>.Success(justAcquired);

            var result = await acquire(cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is { } issued)
                _tokens[KeyFor(configurationName, request)] = issued;

            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public void Invalidate(string configurationName, IdentityTokenRequest request)
    {
        if (request is null)
            return;

        if (_tokens.TryRemove(KeyFor(configurationName, request), out _))
            IdentityLog.TokenCacheInvalidated(_logger, configurationName, request.Audience);
    }

    private bool TryServe(string configurationName, IdentityTokenRequest request, out IssuedIdentityToken token)
    {
        if (_tokens.TryGetValue(KeyFor(configurationName, request), out var candidate)
            && candidate.IsUsableAt(DateTimeOffset.UtcNow, _refreshSkew))
        {
            IdentityLog.TokenServedFromCache(_logger, configurationName, request.Audience, candidate.ExpiresAt);
            token = candidate;
            return true;
        }

        token = null!;
        return false;
    }

    private static string KeyFor(string configurationName, IdentityTokenRequest request)
        => $"{configurationName}{request.CacheKey}";
}
