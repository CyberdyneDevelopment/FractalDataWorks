using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Which provider an <see cref="OidcRedirectStep"/> sends a caller to, and on what terms.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record OidcRedirectStepConfiguration
{
    /// <summary>Gets the provider's issuer, as it appears in the token's <c>iss</c> claim.</summary>
    public required string Issuer { get; init; }

    /// <summary>Gets the provider's authorization endpoint.</summary>
    public required Uri AuthorizationEndpoint { get; init; }

    /// <summary>Gets the provider's token endpoint.</summary>
    public required Uri TokenEndpoint { get; init; }

    /// <summary>Gets where the provider publishes its signing keys.</summary>
    public required Uri JwksUri { get; init; }

    /// <summary>Gets the client identifier this platform registered with the provider.</summary>
    public required string ClientId { get; init; }

    /// <summary>Gets the name of the client secret, resolved from the secret manager.</summary>
    /// <remarks>A name, never a value. A secret in a configuration row is a secret in every backup.</remarks>
    public string? ClientSecretName { get; init; }

    /// <summary>Gets the redirect the provider returns the caller to.</summary>
    /// <remarks>
    /// Registered with the provider by exact string match — no wildcard and no trailing-slash
    /// difference, per RFC 9700. A provider that permits pattern matching is one an open redirector
    /// can be built against.
    /// </remarks>
    public required Uri RedirectUri { get; init; }

    /// <summary>Gets the scopes requested.</summary>
    public IReadOnlyList<string> Scopes { get; init; } = ["openid", "profile", "email"];

    /// <summary>Gets the claim carrying the subject identifier to bind on.</summary>
    /// <remarks>
    /// <c>sub</c> for most providers. <b>Entra needs <c>oid</c></b>: its <c>sub</c> is pairwise —
    /// unique per user <em>and application</em> — so a binding made against it stops matching when
    /// the client changes, and the same person appears as two subjects across two of your apps.
    /// </remarks>
    public string SubjectClaim { get; init; } = "sub";

    /// <summary>Gets the audiences the returned token may carry.</summary>
    public required IReadOnlyList<string> ValidAudiences { get; init; }

    /// <summary>Gets the signing algorithms accepted.</summary>
    public IReadOnlyList<string> ValidAlgorithms { get; init; } = ["RS256"];

    /// <summary>Gets the tolerance for clock difference with the provider.</summary>
    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets the RFC 8176 methods this provider is trusted to assert.</summary>
    /// <remarks>
    /// A ceiling on what the provider's own <c>amr</c> may contribute. The step reads <c>amr</c>
    /// from the returned token and the runner keeps only what also appears here, so a provider
    /// trusted for a password cannot raise assurance by asserting a hardware key.
    /// </remarks>
    public IReadOnlyList<string> AssertableMethods { get; init; } = ["pwd"];
}
