using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// The two columns a use of a session touches on <c>auth.Session</c>. Narrower than
/// <see cref="SessionEntity"/> on purpose: the guarded WHERE clause is what makes the slide safe, and
/// nothing else on the row can be overwritten by it.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SessionUseUpdate
{
    /// <summary>Gets or sets when the session was used.</summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>Gets or sets the idle end this use moves the session to.</summary>
    public DateTimeOffset IdleExpiresAt { get; set; }
}
