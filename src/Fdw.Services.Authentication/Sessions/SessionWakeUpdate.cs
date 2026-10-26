using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>The columns waking a session touches on <c>auth.Session</c>.</summary>
/// <remarks>
/// Waking gives the session a new id: only the hash of the old one is stored, so the browser's cookie cannot be handed back,
/// and an id that was in use while the session slept is not trusted again. A null is written as NULL, so the sleep ends
/// and the CSRF token of the old id is dropped; the caller issues a new one.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SessionWakeUpdate
{
    /// <summary>Gets or sets the hash of the new session id.</summary>
    public byte[] SessionIdHash { get; set; } = [];

    /// <summary>Gets or sets the end of the sleep: always null, which clears it.</summary>
    public DateTimeOffset? AsleepAt { get; set; }

    /// <summary>Gets or sets the CSRF token hash: always null, so the old token no longer works.</summary>
    public byte[]? CsrfTokenHash { get; set; }

    /// <summary>Gets or sets when the session was woken, which counts as its last use.</summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>Gets or sets when the session ends unless it is used first, counted from the wake.</summary>
    public DateTimeOffset IdleExpiresAt { get; set; }
}
