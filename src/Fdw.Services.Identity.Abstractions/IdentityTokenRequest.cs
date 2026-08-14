using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// What a caller is asking an <see cref="IIdentityService"/> for: a token valid at a named audience,
/// carrying a set of scopes.
/// </summary>
/// <remarks>
/// Audience is required and has no default. A token minted for one peer must not be accepted by
/// another, so "which peer is this for" is the question that makes the request meaningful — a
/// defaulted or omitted audience would produce a token whose blast radius nobody declared.
/// </remarks>
public sealed class IdentityTokenRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityTokenRequest"/> class.
    /// </summary>
    /// <param name="audience">The audience the issued token must be valid at. Required.</param>
    /// <param name="scopes">The scopes requested. May be empty when the provider derives scopes from the service account itself.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="audience"/> is null, empty, or whitespace.</exception>
    public IdentityTokenRequest(string audience, IReadOnlyList<string>? scopes = null)
    {
        if (string.IsNullOrWhiteSpace(audience))
            throw new ArgumentException("An identity token request must name the audience it is for.", nameof(audience));

        Audience = audience;
        Scopes = scopes ?? [];
    }

    /// <summary>Gets the audience the issued token must be valid at.</summary>
    public string Audience { get; }

    /// <summary>Gets the requested scopes. Empty means "whatever the service account already carries".</summary>
    public IReadOnlyList<string> Scopes { get; }

    /// <summary>
    /// Gets the cache key for this request — audience plus scopes in a stable order.
    /// </summary>
    /// <remarks>
    /// Scopes are ordered before joining so that two requests asking for the same set in a different
    /// sequence share one cache entry rather than each acquiring its own token.
    /// </remarks>
    public string CacheKey => Scopes.Count == 0
        ? Audience
        : $"{Audience}|{string.Join(" ", Scopes.OrderBy(scope => scope, StringComparer.Ordinal))}";
}
