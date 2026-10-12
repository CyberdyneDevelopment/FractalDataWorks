using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.DevSession.Abstractions;

/// <summary>
/// The front door for dev sessions: opens, finds, sleeps/wakes, and closes them, and administers nested
/// sessions. It owns each session's existence and warm-resource lifecycle (as distinct from coordinating
/// concurrent work inside one, which is <see cref="IWorkspaceCoordinator"/>'s job). Sessions are
/// deduplicated by <see cref="SessionRequest.Key"/> so a fix has exactly one live session regardless of
/// how many workers attach to it.
/// </summary>
public interface IDevSessionManager
{
    /// <summary>
    /// Opens a session for the requested key, materializing its isolated copy. If a live session already
    /// exists for the key, that existing session is returned rather than a second one being created.
    /// </summary>
    /// <param name="request">The session request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the opened (or already-live) session, or a failure.</returns>
    Task<IGenericResult<IDevSession>> Open(SessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a nested session under a parent — a side issue handled while the parent is held — so the two
    /// have distinct isolated copies and ledgers and can later be merged back independently.
    /// </summary>
    /// <param name="parentSessionId">The parent session to nest under.</param>
    /// <param name="request">The nested session request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the opened nested session, or a failure when the parent is unknown.</returns>
    Task<IGenericResult<IDevSession>> OpenNested(Guid parentSessionId, SessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a session by its identifier.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A result containing the session, or a failure when it is unknown.</returns>
    IGenericResult<IDevSession> Get(Guid sessionId);

    /// <summary>
    /// Gets the live session bound to a fix/issue/conversation key, if any.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>A result containing the session, or a failure when no live session holds the key.</returns>
    IGenericResult<IDevSession> Get(string key);

    /// <summary>
    /// Lists all sessions the manager currently holds (live and dormant).
    /// </summary>
    /// <returns>The sessions the manager holds.</returns>
    IReadOnlyList<IDevSession> List();

    /// <summary>
    /// Puts a session to sleep — freeing its warm in-memory resources while retaining its record and
    /// isolated copy so it wakes cheaply.
    /// </summary>
    /// <param name="sessionId">The session to sleep.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the updated session, or a failure.</returns>
    Task<IGenericResult<IDevSession>> Sleep(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wakes a sleeping or hibernated session, rehydrating its warm context and isolated copy.
    /// </summary>
    /// <param name="sessionId">The session to wake.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the woken session, or a failure.</returns>
    Task<IGenericResult<IDevSession>> Wake(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a session — transitioning it to a terminal state and releasing its resources — once its work
    /// has been merged (or abandoned).
    /// </summary>
    /// <param name="sessionId">The session to close.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the closed session, or a failure.</returns>
    Task<IGenericResult<IDevSession>> Close(Guid sessionId, CancellationToken cancellationToken = default);
}
