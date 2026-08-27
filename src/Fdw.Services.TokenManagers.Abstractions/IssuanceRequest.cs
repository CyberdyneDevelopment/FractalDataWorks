using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Exactly what a token is to assert.
/// </summary>
/// <remarks>
/// <para>
/// The runner builds this from a completed authentication context, so the issuer depends on nothing
/// in the authentication pipeline and the pipeline depends on nothing in issuance — only the runner
/// knows both. The alternative, handing the issuer the whole context, would have coupled the two and
/// given issuance access to state it has no business reading.
/// </para>
/// <para>
/// Note what is absent: no grant type, no credential, no redirect URI. Those describe how someone
/// authenticated, which is finished business by the time a token is minted.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record IssuanceRequest
{
    /// <summary>Gets the principal the token names.</summary>
    public required Guid PrincipalId { get; init; }

    /// <summary>Gets the tenant the token is issued within.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Gets the audience the token is minted for.</summary>
    /// <remarks>
    /// Required, and never a wildcard. A token accepted by a service it was not minted for is the
    /// most common real-world break, and it is only preventable if issuance is specific.
    /// </remarks>
    public required string Audience { get; init; }

    /// <summary>Gets the scopes to carry.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }

    /// <summary>Gets the authentication methods actually proved — RFC 8176.</summary>
    /// <remarks>Recorded by the runner from the steps that succeeded, never asserted by a step.</remarks>
    public required IReadOnlyList<string> AuthenticationMethods { get; init; }

    /// <summary>Gets the assurance level those methods amount to.</summary>
    public string? Acr { get; init; }

    /// <summary>Gets the claims to embed.</summary>
    /// <remarks>
    /// The runner selects these, and does not forward a claim merely because an external authority
    /// asserted it. A federated provider naming a role does not thereby grant one.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Claims { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);
}
