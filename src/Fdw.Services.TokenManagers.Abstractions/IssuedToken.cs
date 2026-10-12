using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// What a flow produced.
/// </summary>
/// <remarks>
/// Deliberately outside <c>AuthenticationContext</c>: the flow's product is not something a step may
/// read or write. It is the runner's return value.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record IssuedToken
{
    /// <summary>Gets the access token.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Gets the token type, as the caller must present it — <c>Bearer</c>, <c>DPoP</c>.</summary>
    public required string TokenType { get; init; }

    /// <summary>Gets when the access token expires.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Gets the refresh token, where the flow issues one.</summary>
    /// <remarks>
    /// A refresh token must be rotated with reuse detection or sender-constrained; a long-lived
    /// bearer refresh token is a password with worse ergonomics — RFC 9700.
    /// </remarks>
    public string? RefreshToken { get; init; }
}
