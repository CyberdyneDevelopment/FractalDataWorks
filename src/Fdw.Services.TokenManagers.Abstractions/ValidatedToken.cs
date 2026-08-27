using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// What a presented token establishes, once checked.
/// </summary>
/// <remarks>
/// Separate from the flow's context because it answers a different question: not "who proved what
/// during login" but "what does this artefact entitle its bearer to, right now".
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record ValidatedToken
{
    /// <summary>Gets the principal the token names.</summary>
    public required Guid PrincipalId { get; init; }

    /// <summary>Gets the tenant the token was issued within.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Gets the scopes the token carries.</summary>
    /// <remarks>
    /// A scope constrains what a client may attempt on a principal's behalf. It is not a permission,
    /// which constrains what the principal may do — collapsing the two means a first-party
    /// application needs every scope and the check stops carrying information.
    /// </remarks>
    public required IReadOnlyList<string> Scopes { get; init; }

    /// <summary>Gets the authentication methods the original login proved — RFC 8176.</summary>
    public required IReadOnlyList<string> AuthenticationMethods { get; init; }

    /// <summary>Gets the assurance level reached, for step-up decisions — RFC 9470.</summary>
    public string? Acr { get; init; }

    /// <summary>Gets when the token expires.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
