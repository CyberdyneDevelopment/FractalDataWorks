using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// One row of <c>auth.Session</c> (AuthDb): a signed-in browser held on the server (FDW-903).
/// <see cref="SessionIdHash"/> is the SHA-256 of the id the browser carries, never the id, and
/// <see cref="ClaimsData"/> is the output of an <c>EncryptCommand</c> under the issuing flow's key.
/// </summary>
/// <remarks>
/// Read back through <c>Query.From&lt;SessionEntity&gt;</c>, which needs a generated mapper: without
/// <c>[GenerateMapper]</c> the insert (which binds properties) succeeded and every read failed with
/// "No POCO mapper found", so no session cookie ever authenticated (FDW-918).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class SessionEntity
{
    /// <summary>Gets or sets the session's identity, the <c>sid</c> claim.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the unkeyed SHA-256 of the session id. Never the id itself.</summary>
    public byte[] SessionIdHash { get; set; } = [];

    /// <summary>Gets or sets the Session authentication service the session was issued into.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the flow that authenticated the caller; its key decrypts <see cref="ClaimsData"/>.</summary>
    public string FlowName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user the session is for.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the encrypted, serialized claim set baked at sign-in.</summary>
    public byte[] ClaimsData { get; set; } = [];

    /// <summary>Gets or sets the SHA-256 of the current CSRF token; null until one is issued.</summary>
    public byte[]? CsrfTokenHash { get; set; }

    /// <summary>Gets or sets how far each use moves <see cref="IdleExpiresAt"/>, in seconds.</summary>
    public int IdleTimeoutSeconds { get; set; }

    /// <summary>Gets or sets when the session was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the session was last used.</summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>Gets or sets when the session ends unless it is used first.</summary>
    public DateTimeOffset IdleExpiresAt { get; set; }

    /// <summary>Gets or sets when the session ends however it is used.</summary>
    public DateTimeOffset AbsoluteExpiresAt { get; set; }

    /// <summary>Gets or sets when the session was revoked; null while it is live.</summary>
    public DateTimeOffset? RevokedAt { get; set; }
}
