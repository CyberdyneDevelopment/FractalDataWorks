using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Fetches and caches an authority's published signing keys.
/// </summary>
/// <remarks>
/// <para>
/// Keys rotate, and a cache that only expires on a timer means a rotation blinds verification until
/// it does — twelve hours on Microsoft's defaults, during which every login fails. So the cache also
/// refreshes on demand when a caller reports that nothing it holds verified, rate-limited so a flood
/// of bad tokens cannot turn into a flood of fetches.
/// </para>
/// </remarks>
public sealed class CachingSigningKeyProvider : ISigningKeyProvider
{
    private readonly ConcurrentDictionary<Uri, CacheEntry> _cache = [];
    private readonly HttpClient _http;
    private readonly TimeSpan _lifetime;
    private readonly TimeSpan _minimumRefreshInterval;
    private readonly ILogger<CachingSigningKeyProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="CachingSigningKeyProvider"/> class.</summary>
    /// <param name="http">Fetches the key document.</param>
    /// <param name="lifetime">How long keys are held before a scheduled refresh.</param>
    /// <param name="minimumRefreshInterval">The floor between forced refreshes.</param>
    /// <param name="logger">The logger.</param>
    public CachingSigningKeyProvider(
        HttpClient http,
        TimeSpan? lifetime = null,
        TimeSpan? minimumRefreshInterval = null,
        ILogger<CachingSigningKeyProvider>? logger = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _lifetime = lifetime ?? TimeSpan.FromHours(1);
        _minimumRefreshInterval = minimumRefreshInterval ?? TimeSpan.FromMinutes(5);
        _logger = logger ?? NullLogger<CachingSigningKeyProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyCollection<SecurityKey>>> Current(
        Uri jwksUri, CancellationToken cancellationToken = default)
    {
        if (jwksUri is null)
            return GenericResult<IReadOnlyCollection<SecurityKey>>.Failure(
                SigningKeyLog.UriMissing(_logger));

        if (_cache.TryGetValue(jwksUri, out var entry) && entry.FetchedAt.Add(_lifetime) > DateTimeOffset.UtcNow)
            return GenericResult<IReadOnlyCollection<SecurityKey>>.Success(entry.Keys);

        return await Fetch(jwksUri, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Forces a refresh, for when nothing currently held verified a signature.</summary>
    /// <param name="jwksUri">Where the authority publishes its keys.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Rate-limited: without a floor, a stream of tokens signed by a key that will never exist turns
    /// into a stream of requests at the identity provider, which is a denial of service anyone can
    /// trigger by sending rubbish.
    /// </remarks>
    public async Task<IGenericResult<IReadOnlyCollection<SecurityKey>>> Refresh(
        Uri jwksUri, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(jwksUri, out var entry)
            && entry.FetchedAt.Add(_minimumRefreshInterval) > DateTimeOffset.UtcNow)
        {
            SigningKeyLog.RefreshThrottled(_logger, jwksUri.ToString());
            return GenericResult<IReadOnlyCollection<SecurityKey>>.Success(entry.Keys);
        }

        return await Fetch(jwksUri, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<IReadOnlyCollection<SecurityKey>>> Fetch(
        Uri jwksUri, CancellationToken cancellationToken)
    {
        try
        {
            var document = await _http.GetStringAsync(jwksUri, cancellationToken).ConfigureAwait(false);
            var keys = new JsonWebKeySet(document).GetSigningKeys();

            if (keys.Count == 0)
                return GenericResult<IReadOnlyCollection<SecurityKey>>.Failure(
                    SigningKeyLog.NoKeysPublished(_logger, jwksUri.ToString()));

            _cache[jwksUri] = new CacheEntry([.. keys], DateTimeOffset.UtcNow);
            SigningKeyLog.Fetched(_logger, jwksUri.ToString(), keys.Count);

            return GenericResult<IReadOnlyCollection<SecurityKey>>.Success([.. keys]);
        }
        catch (HttpRequestException ex)
        {
            // Why caught rather than propagated: the identity provider being unreachable is an
            // expected operational condition, and a login should fail as a login rather than as an
            // unhandled exception several layers up.
            return GenericResult<IReadOnlyCollection<SecurityKey>>.Failure(
                SigningKeyLog.FetchFailed(_logger, jwksUri.ToString(), ex.GetType().Name));
        }
    }

    private sealed record CacheEntry(IReadOnlyCollection<SecurityKey> Keys, DateTimeOffset FetchedAt);
}
