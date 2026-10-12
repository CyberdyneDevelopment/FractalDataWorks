using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Provides persistence for session data in the system store.
/// </summary>
/// <remarks>
/// <para>
/// The session store persists full session data to the file system.
/// Default locations by platform:
/// <list type="bullet">
/// <item><description>Linux: ~/.local/share/roslyn-mcp/sessions/</description></item>
/// <item><description>Windows: %LOCALAPPDATA%/roslyn-mcp/sessions/</description></item>
/// <item><description>macOS: ~/Library/Application Support/roslyn-mcp/sessions/</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ISessionStore
{
    /// <summary>
    /// Gets the base path where sessions are stored.
    /// </summary>
    string BasePath { get; }

    /// <summary>
    /// Loads a session from the store.
    /// </summary>
    /// <param name="sessionId">The session ID to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted session, or null if not found.</returns>
    Task<PersistedSession?> LoadSession(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a session to the store.
    /// </summary>
    /// <param name="session">The session to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult<bool>> SaveSession(
        PersistedSession session,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session from the store.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult<bool>> DeleteSession(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all sessions in the store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of session info for all persisted sessions.</returns>
    Task<IReadOnlyList<SessionInfo>> ListSessions(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file path for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The full file path for the session file.</returns>
    string GetSessionPath(Guid sessionId);

    /// <summary>
    /// Checks if a session exists in the store.
    /// </summary>
    /// <param name="sessionId">The session ID to check.</param>
    /// <returns>True if the session exists.</returns>
    bool SessionExists(Guid sessionId);

    /// <summary>
    /// Ensures the store directory exists.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    IGenericResult<bool> EnsureStoreExists();
}