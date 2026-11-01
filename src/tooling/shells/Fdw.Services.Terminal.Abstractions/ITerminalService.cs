using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Terminal;

/// <summary>
/// Service for managing persistent terminal sessions.
/// </summary>
public interface ITerminalService
{
    /// <summary>
    /// Creates a new terminal session for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The session name.</param>
    /// <param name="command">The command to run (e.g., /bin/bash, pwsh, claude).</param>
    /// <param name="args">Optional command arguments.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created session.</returns>
    Task<IGenericResult<ITerminalSession>> CreateSession(
        Guid userId,
        string name,
        string command,
        string[]? args = null,
        string? workingDirectory = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets an existing terminal session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The session if found, null otherwise.</returns>
    Task<IGenericResult<ITerminalSession?>> GetSession(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active sessions for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of active sessions.</returns>
    Task<IGenericResult<IReadOnlyList<ITerminalSession>>> GetUserSessions(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Terminates a terminal session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> TerminateSession(Guid sessionId, CancellationToken ct = default);
}
