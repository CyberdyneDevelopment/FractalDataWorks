using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>The single column revoking a session touches on <c>auth.Session</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed class SessionRevokeUpdate
{
    /// <summary>Gets or sets when the session was revoked.</summary>
    public DateTimeOffset RevokedAt { get; set; }
}
