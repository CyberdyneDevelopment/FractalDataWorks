using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Starts a session by acting with a role (FDW-2282): a new session that asserts <paramref name="assertion"/>, linked to the session it
    /// came from, which is put to sleep.
    /// </summary>
    /// <param name="parentSessionId">The session the person is acting from: live and awake.</param>
    /// <param name="assertion">What the acting session asserts. The caller has already decided it is no wider than the parent's grant.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The acting session as an issued token, as <see cref="Create"/> returns one. If the parent cannot be put to sleep the new session is
    /// revoked again and this fails. The new session never outlives the parent.
    /// </returns>
    Task<IGenericResult<IssuedToken>> Act(Guid parentSessionId, IssuanceRequest assertion, CancellationToken cancellationToken = default);

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
    /// Puts a live session to sleep: it is refused on every request, exactly as a revoked one is, until it is woken (FDW-2282).
    /// </summary>
    /// <param name="id">The session's <see cref="SessionRecord.Id"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// A session sleeps while a session started from it by acting with a role is in use. Sleeping a session that is revoked, expired or
    /// already asleep is a failure, not a success.
    /// </remarks>
    Task<IGenericResult> Sleep(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wakes a sleeping session and gives it a new random id, because only the hash of the old id is stored and an id that was
    /// presented while the session slept is not trusted again.
    /// </summary>
    /// <param name="id">The session's <see cref="SessionRecord.Id"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The session as an issued token, as <see cref="Create"/> returns one: <see cref="IssuedToken.AccessToken"/> is the new id the
    /// browser carries. Its CSRF token is cleared, so the caller issues a new one.
    /// </returns>
    Task<IGenericResult<IssuedToken>> Wake(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns from acting (FDW-2282): ends every acting session above a step and wakes that step.
    /// </summary>
    /// <param name="actingSessionId">The acting session the request was made with.</param>
    /// <param name="step">The step to return to: 0 is the person's own session, the root of the chain.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The woken session as an issued token, as <see cref="Wake"/> returns one. Fails, ending nothing, when the session is not acting or
    /// the step is not an ancestor of it. The sessions above the step are ended top first, then the step is woken.
    /// </returns>
    Task<IGenericResult<IssuedToken>> Return(Guid actingSessionId, int step, CancellationToken cancellationToken = default);

    /// <summary>The chain of sessions a session was started through, from the person's own session to this one.</summary>
    /// <param name="sessionId">The session's <see cref="SessionRecord.Id"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The steps in order, step 0 first. A session that is not acting has just step 0.</returns>
    Task<IGenericResult<IReadOnlyList<ActingStep>>> Chain(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a new CSRF token for a session, replacing any earlier one, and returns it once.
    /// </summary>
    /// <param name="id">The session's <see cref="SessionRecord.Id"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The token. Only its hash is stored, so it cannot be read back later.</returns>
    Task<IGenericResult<string>> IssueCsrfToken(Guid id, CancellationToken cancellationToken = default);
}
