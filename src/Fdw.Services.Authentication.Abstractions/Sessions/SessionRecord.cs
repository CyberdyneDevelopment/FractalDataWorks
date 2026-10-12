using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Sessions;

/// <summary>
/// A live server-side session, as <see cref="ISessionStore.Use"/> resolved it for one request.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record SessionRecord
{
    /// <summary>The <see cref="IssuedToken.TokenType"/> a session is issued as.</summary>
    public const string TokenType = "Session";

    /// <summary>Gets the session's identity: the <c>sid</c> claim, and what logout revokes.</summary>
    /// <remarks>Never the id the browser carries; that is stored only as a hash.</remarks>
    public required Guid Id { get; init; }

    /// <summary>Gets the Session authentication service the session was issued into.</summary>
    public required string ServiceName { get; init; }

    /// <summary>Gets the user the session is for.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets when the session was created: its issued-at for user-wide revocation.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when the session ends however active it stays.</summary>
    public required DateTimeOffset AbsoluteExpiresAt { get; init; }

    /// <summary>Gets what the session asserts: the claims baked at sign-in.</summary>
    public required IssuanceRequest Assertion { get; init; }

    /// <summary>Gets the SHA-256 of the session's current CSRF token, or null when none was issued.</summary>
    public byte[]? CsrfTokenHash { get; init; }
}
