using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// Holds issued tokens so an outbound call reuses a live token instead of acquiring a new one every
/// time.
/// </summary>
/// <remarks>
/// <para>
/// Without this, the identity provider sits on the hot path of every outbound request and becomes a
/// hard dependency of every call — a provider hiccup would take out all service-to-service traffic
/// rather than only traffic whose token had aged out.
/// </para>
/// <para>
/// Entries are keyed by identity-configuration name plus <see cref="IdentityTokenRequest.CacheKey"/>
/// (audience and ordered scopes), because a token valid at one audience must never be handed to a
/// call bound for another.
/// </para>
/// <para>
/// Implementations hold tokens in memory only and never persist them. A token is a bearer
/// credential with a short life; writing it anywhere durable creates a longer-lived secret than the
/// static one this domain exists to remove.
/// </para>
/// </remarks>
public interface IIdentityTokenCache
{
    /// <summary>
    /// Returns a cached token that is still usable, acquiring one via <paramref name="acquire"/> only
    /// when there is no live entry.
    /// </summary>
    /// <param name="configurationName">The identity configuration whose token this is.</param>
    /// <param name="request">The request whose audience and scopes key the entry.</param>
    /// <param name="acquire">Invoked to obtain a fresh token when no usable entry exists.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>A usable token, or the failure that <paramref name="acquire"/> returned.</returns>
    Task<IGenericResult<IssuedIdentityToken>> GetOrAcquire(
        string configurationName,
        IdentityTokenRequest request,
        Func<CancellationToken, Task<IGenericResult<IssuedIdentityToken>>> acquire,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops the cached entry for <paramref name="configurationName"/> and <paramref name="request"/>,
    /// so the next call acquires a fresh token.
    /// </summary>
    /// <param name="configurationName">The identity configuration whose entry is dropped.</param>
    /// <param name="request">The request identifying the entry.</param>
    /// <remarks>
    /// Call this when a peer rejects a token that had not yet reached its expiry — the provider may
    /// have revoked it, and the cache would otherwise keep serving a token that is known to fail
    /// until the clock catches up.
    /// </remarks>
    void Invalidate(string configurationName, IdentityTokenRequest request);
}
