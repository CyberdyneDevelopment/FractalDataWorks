using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Workspace.Management;

/// <summary>
/// Abstracts the persistence mechanism for workspace sessions.
/// </summary>
/// <remarks>
/// Implementations can store sessions in files, databases, or other storage mechanisms.
/// The session store is responsible for serializing and deserializing <see cref="WorkspaceSession"/>
/// objects for durability across application restarts.
/// </remarks>
public interface IWorkspaceSessionStore
{
    /// <summary>
    /// Saves a workspace session to the store.
    /// </summary>
    /// <param name="session">The session to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult<bool>> Save(WorkspaceSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a workspace session from the store.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the session on success.</returns>
    Task<IGenericResult<WorkspaceSession>> Load(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session from the store.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult<bool>> Delete(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all sessions in the store.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about all stored sessions.</returns>
    Task<IEnumerable<SessionInfo>> List(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session exists in the store.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the session exists.</returns>
    Task<bool> Exists(Guid sessionId, CancellationToken cancellationToken = default);
}
