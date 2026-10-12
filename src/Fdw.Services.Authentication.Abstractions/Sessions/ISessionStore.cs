using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Sessions;

/// <summary>
/// Holds server-side sessions: what a signed-in browser is, kept on the server and reached through a
/// random id the browser carries in a cookie (FDW-903).
/// </summary>
/// <remarks>
/// <para>
/// A session-delivered flow ends here instead of at a token issuer. The claims a bearer token from
/// the same flow would have carried are stored, encrypted under that flow's key, and the browser is
/// handed only a random 256-bit id. There is nothing in the browser to read and nothing to alter: a
/// changed id matches no row.
/// </para>
/// <para>
/// A session presented for use fails identically whether it never existed, has expired, was
/// revoked, or belongs to another service, so a caller cannot learn that an id was ever real.
/// </para>
/// </remarks>
public interface ISessionStore
{
    /// <summary>Creates a session asserting what <paramref name="assertion"/> names.</summary>
    /// <param name="serviceName">The Session authentication service the session is issued into.</param>
    /// <param name="flowName">The flow that authenticated the caller. Its key encrypts the stored claims.</param>
    /// <param name="assertion">What the session asserts: the same request a bearer token would be minted from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The session as an issued token: <see cref="IssuedToken.AccessToken"/> is the session id the
    /// browser carries, <see cref="IssuedToken.TokenType"/> is <see cref="SessionRecord.TokenType"/>,
    /// and <see cref="IssuedToken.ExpiresAt"/> is the absolute end, which use never extends.
    /// </returns>
    Task<IGenericResult<IssuedToken>> Create(
        string serviceName, string flowName, IssuanceRequest assertion, CancellationToken cancellationToken = default);

    /// <summary>Resolves a presented session id for one request, and extends its idle expiry.</summary>
    /// <param name="sessionId">The id the browser presented.</param>
    /// <param name="serviceName">The service the id was presented to; a session of another service is refused.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The live session, or a failure that does not say why.</returns>
    Task<IGenericResult<SessionRecord>> Use(
        string sessionId, string serviceName, CancellationToken cancellationToken = default);

    /// <summary>Ends a session. Nothing presented afterwards under its id authenticates.</summary>
    /// <param name="id">The session's <see cref="SessionRecord.Id"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult> Revoke(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a new CSRF token for a session, replacing any earlier one, and returns it once.
    /// </summary>
    /// <param name="id">The session's <see cref="SessionRecord.Id"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The token. Only its hash is stored, so it cannot be read back later.</returns>
    Task<IGenericResult<string>> IssueCsrfToken(Guid id, CancellationToken cancellationToken = default);
}
