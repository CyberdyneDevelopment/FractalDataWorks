using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Manages multiple workspace sessions with lifecycle management including
/// persistence, sleep/wake functionality, and conversation tracking.
/// </summary>
/// <remarks>
/// <para>
/// The session manager provides a layer above workspace management that adds:
/// <list type="bullet">
/// <item><description>Session persistence to disk</description></item>
/// <item><description>Conversation ID tracking for Claude resume</description></item>
/// <item><description>Project-level session index (.claude/roslyn.sessions)</description></item>
/// <item><description>Multiple isolated sessions per solution</description></item>
/// </list>
/// </para>
/// <para>
/// Sessions are stored in two tiers:
/// <list type="bullet">
/// <item><description>Project index: .claude/roslyn.sessions (lightweight, can be committed)</description></item>
/// <item><description>System store: ~/.local/share/roslyn-mcp/sessions/ (full data, not in git)</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ISessionManager : IDisposable
{
    /// <summary>
    /// Gets the ID of the currently active session, or null if none.
    /// </summary>
    Guid? ActiveSessionId { get; }

    /// <summary>
    /// Gets the workspace for the active session, or null if no session is active.
    /// </summary>
    IRoslynWorkspace? ActiveWorkspace { get; }

    /// <summary>
    /// Gets the sleep timeout duration. Sessions inactive for longer than this
    /// will have their workspaces put to sleep.
    /// </summary>
    TimeSpan SleepTimeout { get; }

    /// <summary>
    /// Creates a new session for a solution.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln or .slnx file.</param>
    /// <param name="description">Human-readable description of the session.</param>
    /// <param name="conversationId">Optional conversation ID for Claude resume.</param>
    /// <param name="setAsActive">If true, sets this session as active.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session info, or a failure result.</returns>
    Task<IGenericResult<SessionInfo>> CreateSession(
        string solutionPath,
        string description,
        string? conversationId = null,
        bool setAsActive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new session for a solution with project filtering.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln or .slnx file.</param>
    /// <param name="description">Human-readable description of the session.</param>
    /// <param name="excludePatterns">
    /// Glob patterns for projects to exclude (e.g., "*.Tests", "*.Benchmarks").
    /// Use <see cref="DefaultExcludePatterns.TestProjects"/> to exclude common test projects.
    /// </param>
    /// <param name="conversationId">Optional conversation ID for Claude resume.</param>
    /// <param name="setAsActive">If true, sets this session as active.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session info, or a failure result.</returns>
    Task<IGenericResult<SessionInfo>> CreateSession(
        string solutionPath,
        string description,
        IReadOnlyList<string> excludePatterns,
        string? conversationId = null,
        bool setAsActive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a previously persisted session.
    /// </summary>
    /// <param name="sessionId">The session ID to resume.</param>
    /// <param name="setAsActive">If true, sets this session as active.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resumed session info, or a failure result.</returns>
    Task<IGenericResult<SessionInfo>> ResumeSession(
        Guid sessionId,
        bool setAsActive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a session to the system store.
    /// </summary>
    /// <param name="sessionId">The session ID to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, or a failure result.</returns>
    Task<IGenericResult<bool>> SaveSession(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a session and optionally saves it.
    /// </summary>
    /// <param name="sessionId">The session ID to close.</param>
    /// <param name="save">If true, saves before closing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, or a failure result.</returns>
    Task<IGenericResult<bool>> CloseSession(
        Guid sessionId,
        bool save = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the workspace for a session synchronously, waking it if necessary.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The workspace, or a failure result.</returns>
    IGenericResult<IRoslynWorkspace> GetSessionWorkspaceSync(Guid sessionId);

    /// <summary>
    /// Gets the workspace for a session, waking it if necessary.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workspace, or null if not found.</returns>
    Task<IRoslynWorkspace?> GetSessionWorkspace(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a session as the active session.
    /// </summary>
    /// <param name="sessionId">The session ID to make active.</param>
    /// <returns>True if successful.</returns>
    bool SetActiveSession(Guid sessionId);

    /// <summary>
    /// Updates session metadata.
    /// </summary>
    /// <param name="sessionId">The session ID to update.</param>
    /// <param name="description">New description (null to keep existing).</param>
    /// <param name="conversationId">New conversation ID (null to keep existing).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated session info, or a failure result.</returns>
    Task<IGenericResult<SessionInfo>> UpdateSessionMetadata(
        Guid sessionId,
        string? description = null,
        string? conversationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active (in-memory) sessions.
    /// </summary>
    /// <returns>List of active session info.</returns>
    IReadOnlyList<SessionInfo> ListActiveSessions();

    /// <summary>
    /// Lists all persisted sessions from the system store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of persisted session info.</returns>
    Task<IReadOnlyList<SessionInfo>> ListPersistedSessions(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a persisted session from the system store.
    /// </summary>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, or a failure result.</returns>
    Task<IGenericResult<bool>> DeletePersistedSession(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Puts a session's workspace to sleep to conserve memory.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>True if successful.</returns>
    bool SleepSession(Guid sessionId);

    /// <summary>
    /// Wakes a sleeping session's workspace.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The awakened workspace, or null if not found.</returns>
    Task<IRoslynWorkspace?> WakeSession(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks all sessions and puts inactive ones to sleep.
    /// </summary>
    void CheckAndSleepInactiveSessions();

    /// <summary>
    /// Updates the project session index file (.claude/roslyn.sessions).
    /// </summary>
    /// <param name="projectPath">Path to the project directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateProjectSessionIndex(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the project session index from a project directory.
    /// </summary>
    /// <param name="projectPath">Path to the project directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The project session index, or null if not found.</returns>
    Task<ProjectSessionIndex?> LoadProjectSessionIndex(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session info by ID from active sessions.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>Session info, or null if not found.</returns>
    SessionInfo? GetSessionInfo(Guid sessionId);

    /// <summary>
    /// Finds a session by conversation ID.
    /// </summary>
    /// <param name="conversationId">The conversation ID to find.</param>
    /// <returns>Session info, or null if not found.</returns>
    /// <remarks>
    /// Searches only sessions live in THIS process. To find a session created by an earlier process —
    /// which is the case reattach-after-reconnect actually needs — use the overload taking a
    /// <see cref="CancellationToken"/>, which also consults the persisted store.
    /// </remarks>
    SessionInfo? FindSessionByConversationId(string conversationId);

    /// <summary>
    /// Finds a session by conversation ID, including sessions persisted by earlier processes.
    /// </summary>
    /// <param name="conversationId">The conversation ID to find.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session info, or null if not found.</returns>
    /// <remarks>
    /// Checks live sessions first, then the persisted store. This is the overload a host should use
    /// to decide between resuming an agent's existing session and creating a new one, because the
    /// in-memory-only overload cannot see across a process restart.
    /// </remarks>
    Task<SessionInfo?> FindSessionByConversationId(
        string conversationId,
        CancellationToken cancellationToken);
}
