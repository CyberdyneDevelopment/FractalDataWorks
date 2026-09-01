using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Which external authority a <see cref="ForeignTokenStep"/> will trust, and on what terms.
/// </summary>
/// <remarks>
/// One instance per provider. Entra, Auth0 and Authentik are all OIDC, so they are this same step
/// configured three times rather than three steps.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record ForeignTokenStepConfiguration
{
    /// <summary>Gets the issuer, exactly as it appears in the token's <c>iss</c> claim.</summary>
    /// <remarks>
    /// Compared by ordinal equality. Auth0 issues with a trailing slash and Entra does not, so a
    /// value copied from a portal without checking is the usual cause of a token that verifies
    /// cryptographically and is then rejected.
    /// </remarks>
    public required string Issuer { get; init; }

    /// <summary>Gets where the signing keys are published.</summary>
    public required Uri JwksUri { get; init; }

    /// <summary>Gets the audiences a token may carry to be accepted here.</summary>
    /// <remarks>
    /// Required and never empty. A token minted for another audience that verifies here is the most
    /// common real-world break, and only an explicit list prevents it.
    /// </remarks>
    public required IReadOnlyList<string> ValidAudiences { get; init; }

    /// <summary>Gets the signing algorithms accepted.</summary>
    /// <remarks>
    /// Pinned rather than read from the token header. A verifier that accepts whatever the token
    /// names is how <c>none</c> and RS256-to-HS256 confusion work.
    /// </remarks>
    public required IReadOnlyList<string> ValidAlgorithms { get; init; }

    /// <summary>Gets the tolerance for clock difference between here and the issuer.</summary>
    /// <remarks>
    /// Thirty seconds, not the five minutes Microsoft's handler defaults to — five means a revoked
    /// or expired token keeps working for five more.
    /// </remarks>
    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets the RFC 8176 methods this provider is trusted to assert.</summary>
    /// <remarks>
    /// <para>
    /// A ceiling on what the provider's own <c>amr</c> may contribute, not a claim about what it
    /// did. The step reads <c>amr</c> from the token — the provider is the only authority on how
    /// someone proved themselves to it — and the runner keeps only the values that also appear here.
    /// </para>
    /// <para>
    /// So a provider you trust for a password cannot raise your assurance by asserting a hardware
    /// key, whether through misconfiguration or compromise. Widen this only for a provider whose
    /// enforcement of those methods you have actually checked.
    /// </para>
    /// </remarks>
    public required IReadOnlyList<string> AssertableMethods { get; init; }
}
