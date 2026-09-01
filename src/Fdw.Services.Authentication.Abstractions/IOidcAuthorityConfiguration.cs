using System;
using System.Collections.Generic;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Which OIDC provider the OidcRedirect step sends a caller to, and on what terms.
/// </summary>
/// <remarks>
/// The mechanism - challenge with PKCE, exchange the code, verify the returned token - is identical
/// for Auth0, Entra or Authentik. Only these values differ, which is why the step lives in the
/// framework and an implementation of this interface lives with whichever provider a deployment
/// chose.
/// </remarks>
public interface IOidcAuthorityConfiguration
{
    /// <summary>Gets the provider's issuer, as it appears in the token's <c>iss</c> claim.</summary>
    string Issuer { get; }

    /// <summary>Gets the provider's authorization endpoint.</summary>
    Uri AuthorizationEndpoint { get; }

    /// <summary>Gets the provider's token endpoint.</summary>
    Uri TokenEndpoint { get; }

    /// <summary>Gets where the provider publishes its signing keys.</summary>
    Uri JwksUri { get; }

    /// <summary>Gets the client identifier this platform registered with the provider.</summary>
    string ClientId { get; }

    /// <summary>Gets the redirect the provider returns the caller to.</summary>
    /// <remarks>
    /// Registered with the provider by exact string match - no wildcard and no trailing-slash
    /// difference, per RFC 9700. A provider that permits pattern matching is one an open redirector
    /// can be built against.
    /// </remarks>
    Uri RedirectUri { get; }

    /// <summary>Gets the scopes requested.</summary>
    IReadOnlyList<string> Scopes { get; }

    /// <summary>Gets the claim carrying the subject identifier to bind on.</summary>
    /// <remarks>
    /// <c>sub</c> for most providers. <b>Entra needs <c>oid</c></b>: its <c>sub</c> is pairwise -
    /// unique per user <em>and</em> application - so a binding made against it stops matching when
    /// the client changes, and the same person appears as two subjects across two of your apps.
    /// </remarks>
    string SubjectClaim { get; }

    /// <summary>Gets the audiences the returned token may carry.</summary>
    IReadOnlyList<string> ValidAudiences { get; }

    /// <summary>Gets the signing algorithms accepted.</summary>
    IReadOnlyList<string> ValidAlgorithms { get; }

    /// <summary>Gets the tolerance for clock difference with the provider.</summary>
    TimeSpan ClockSkew { get; }

    /// <summary>Gets the RFC 8176 methods this provider is trusted to assert.</summary>
    /// <remarks>
    /// A ceiling on what the provider's own <c>amr</c> may contribute. The step reads <c>amr</c>
    /// from the returned token and the runner keeps only what also appears here, so a provider
    /// trusted for a password cannot raise assurance by asserting a hardware key.
    /// </remarks>
    IReadOnlyList<string> AssertableMethods { get; }
}
