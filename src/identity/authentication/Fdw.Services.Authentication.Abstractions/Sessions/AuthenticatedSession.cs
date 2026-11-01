using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Fdw.Services.Authentication.Abstractions.Sessions;

/// <summary>
/// A session that still stands, and the principal it stands for.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AuthenticatedSession
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticatedSession"/> class.</summary>
    /// <param name="session">The live session, as the store answered it.</param>
    /// <param name="principal">The principal built from the claims baked at sign-in.</param>
    public AuthenticatedSession(SessionRecord session, ClaimsPrincipal principal)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Principal = principal ?? throw new ArgumentNullException(nameof(principal));
    }

    /// <summary>Gets the live session.</summary>
    public SessionRecord Session { get; }

    /// <summary>Gets the principal the session stands for.</summary>
    public ClaimsPrincipal Principal { get; }
}
