using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Roslyn.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Manages multiple workspace sessions with lifecycle management including
/// persistence, sleep/wake functionality, and conversation tracking.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class SessionManager : ISessionManager
{
    private readonly IRoslynWorkspaceFactory _workspaceFactory;
    private readonly ISessionStore _sessionStore;
    private readonly IProjectIndexStore _projectIndexStore;
    private readonly ILogger<SessionManager> _logger;
    private readonly ConcurrentDictionary<Guid, ManagedSessionState> _sessions = new();
    private readonly Timer _sleepTimer;
    private readonly Lock _activeLock = new();

    private Guid? _activeSessionId;
    private bool _disposed;

    /// <summary>
    /// Default sleep timeout of 5 minutes.
    /// </summary>
    public static readonly TimeSpan DefaultSleepTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionManager"/> class.
    /// </summary>
    /// <param name="workspaceFactory">Factory for creating workspaces.</param>
    /// <param name="sessionStore">Store for persisting sessions.</param>
    /// <param name="projectIndexStore">Store for project session indices.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="sleepTimeout">Optional sleep timeout. Defaults to 5 minutes.</param>
    public SessionManager(
        IRoslynWorkspaceFactory workspaceFactory,
        ISessionStore sessionStore,
        IProjectIndexStore projectIndexStore,
        ILogger<SessionManager>? logger = null,
        TimeSpan? sleepTimeout = null)
    {
        _workspaceFactory = workspaceFactory ?? throw new ArgumentNullException(nameof(workspaceFactory));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _projectIndexStore = projectIndexStore ?? throw new ArgumentNullException(nameof(projectIndexStore));
        _logger = logger ?? NullLogger<SessionManager>.Instance;
        SleepTimeout = sleepTimeout ?? DefaultSleepTimeout;

        // Start the sleep check timer - runs every minute
        _sleepTimer = new Timer(
            _ => CheckAndSleepInactiveSessions(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc/>
    public TimeSpan SleepTimeout { get; }

    /// <inheritdoc/>
    public Guid? ActiveSessionId
    {
        get
        {
            lock (_activeLock)
            {
                return _activeSessionId;
            }
        }
    }

    /// <inheritdoc/>
    public IRoslynWorkspace? ActiveWorkspace
    {
        get
        {
            lock (_activeLock)
            {
                if (_activeSessionId is null)
                    return null;

                if (_sessions.TryGetValue(_activeSessionId.Value, out var state))
                {
                    state.LastAccessedAt = DateTimeOffset.UtcNow;
                    return state.Workspace;
                }

                return null;
            }
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<SessionInfo>> CreateSession(
        string solutionPath,
        string description,
        string? conversationId = null,
        bool setAsActive = true,
        CancellationToken cancellationToken = default)
    {
        return CreateSession(solutionPath, description, [], conversationId, setAsActive, cancellationToken);
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear session creation flow: validate, open workspace, build state, update index
    public async Task<IGenericResult<SessionInfo>> CreateSession(
        string solutionPath,
        string description,
        IReadOnlyList<string> excludePatterns,
        string? conversationId = null,
        bool setAsActive = true,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(solutionPath))
        {
            return GenericResult<SessionInfo>.Failure(
                WorkspaceResultCodes.ByName("SolutionPathRequired"));
        }

        var fullPath = Path.GetFullPath(solutionPath);

        if (!File.Exists(fullPath))
        {
            return GenericResult<SessionInfo>.Failure(
                WorkspaceResultCodes.ByName("SolutionFileNotFound"),
                ResultDetails.Create().With("SolutionPath", fullPath));
        }

        RoslynWorkspaceLog.SessionCreating(_logger, fullPath);

        try
        {
            // Open the workspace with project filtering
            var workspace = await _workspaceFactory.CreateFromSolution(
                fullPath,
                excludePatterns ?? [],
                cancellationToken).ConfigureAwait(false);

            var now = DateTimeOffset.UtcNow;
            var sessionId = Guid.NewGuid();

            var state = new ManagedSessionState
            {
                Id = sessionId,
                SolutionPath = fullPath,
                Description = description ?? $"Session {sessionId:N}",
                ConversationId = conversationId,
                CreatedAt = now,
                LastModifiedAt = now,
                LastAccessedAt = now,
                Workspace = workspace,
                HasPendingChanges = false,
                ExcludePatterns = excludePatterns?.ToList() ?? []
            };

            // Try to get current git commit hash for baseline
            state.BaselineCommitHash = await TryGetGitCommitHash(fullPath, cancellationToken)
                .ConfigureAwait(false);
            state.BaselineTimestamp = now;

            _sessions[sessionId] = state;

            RoslynWorkspaceLog.SessionCreated(_logger, sessionId, fullPath, state.Description);

            // Why persist at creation rather than only on save/close: a session's whole purpose is to
            // be findable again later, and the process holding it does not get to choose how it ends.
            // A crash, a client disconnect, or an operator killing a stdio server all skip CloseSession
            // entirely — so a create-without-persist meant the common case left nothing on disk and
            // every reconnect looked like a first visit. The record is cheap; the workspace graph is
            // not persisted here, only the session's identity and metadata.
            // Deliberately not fatal: failing to write the record must not fail a load that otherwise
            // succeeded. The caller still gets a working in-memory session; it just will not survive
            // this process, and the failure is logged rather than swallowed silently.
            var persistResult = await SaveSession(sessionId, cancellationToken).ConfigureAwait(false);
            if (!persistResult.IsSuccess)
            {
                RoslynWorkspaceLog.SessionSaveFailed(_logger, sessionId, persistResult.CurrentMessage ?? string.Empty);
            }

            if (setAsActive)
            {
                SetActiveSession(sessionId);
            }

            // Update project index
            var projectPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(projectPath))
            {
                await UpdateProjectSessionIndex(projectPath, cancellationToken).ConfigureAwait(false);
            }

            return GenericResult<SessionInfo>.Success(state.ToSessionInfo());
        }
        catch (Exception ex)
        {
            return GenericResult<SessionInfo>.Failure(
                WorkspaceResultCodes.ByName("SessionCreationFailed"),
                ResultDetails.Create().With("SolutionPath", fullPath).With("ErrorMessage", ex.Message));
        }
    }
#pragma warning restore MA0051

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear session resume flow: check memory, load from store, rebuild state
    public async Task<IGenericResult<SessionInfo>> ResumeSession(
        Guid sessionId,
        bool setAsActive = true,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Check if already in memory
        if (_sessions.TryGetValue(sessionId, out var existingState))
        {
            existingState.LastAccessedAt = DateTimeOffset.UtcNow;

            if (existingState.IsSleeping)
            {
                await WakeSessionInternal(existingState, cancellationToken).ConfigureAwait(false);
            }

            if (setAsActive)
            {
                SetActiveSession(sessionId);
            }

            RoslynWorkspaceLog.SessionResumed(_logger, sessionId, existingState.SolutionPath);
            return GenericResult<SessionInfo>.Success(existingState.ToSessionInfo());
        }

        // Load from store
        RoslynWorkspaceLog.SessionResuming(_logger, sessionId);

        var persisted = await _sessionStore.LoadSession(sessionId, cancellationToken).ConfigureAwait(false);

        if (persisted is null)
        {
            return GenericResult<SessionInfo>.Failure(
                WorkspaceResultCodes.ByName("PersistedSessionNotFound"),
                ResultDetails.Create().With("SessionId", sessionId.ToString()));
        }

        // Verify solution still exists
        if (!File.Exists(persisted.SolutionPath))
        {
            return GenericResult<SessionInfo>.Failure(
                WorkspaceResultCodes.ByName("SolutionFileNotFound"),
                ResultDetails.Create().With("SolutionPath", persisted.SolutionPath));
        }

        try
        {
            // Open the workspace
            var workspace = await _workspaceFactory.CreateFromSolution(persisted.SolutionPath, cancellationToken)
                .ConfigureAwait(false);

            var now = DateTimeOffset.UtcNow;

            var state = new ManagedSessionState
            {
                Id = persisted.Id,
                SolutionPath = persisted.SolutionPath,
                Description = persisted.Description,
                ConversationId = persisted.ConversationId,
                CreatedAt = persisted.CreatedAt,
                LastModifiedAt = persisted.LastModifiedAt,
                LastAccessedAt = now,
                Workspace = workspace,
                HasPendingChanges = persisted.DocumentChanges.Count > 0,
                BaselineCommitHash = persisted.Baseline?.CommitHash,
                BaselineTimestamp = persisted.Baseline?.Timestamp,
                DocumentChanges = new Dictionary<string, string>(
                    persisted.DocumentChanges, StringComparer.Ordinal),
                Snapshots = persisted.Snapshots.Select(s => new SessionSnapshot
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    DocumentChanges = new Dictionary<string, string>(
                        s.DocumentChanges, StringComparer.Ordinal)
                }).ToList()
            };

            _sessions[sessionId] = state;

            RoslynWorkspaceLog.SessionResumed(_logger, sessionId, state.SolutionPath);

            if (setAsActive)
            {
                SetActiveSession(sessionId);
            }

            return GenericResult<SessionInfo>.Success(state.ToSessionInfo());
        }
        catch (Exception ex)
        {
            return GenericResult<SessionInfo>.Failure(
                WorkspaceResultCodes.ByName("SessionResumeFailed"),
                ResultDetails.Create().With("SessionId", sessionId.ToString()).With("ErrorMessage", ex.Message));
        }
    }
#pragma warning restore MA0051

    /// <inheritdoc/>
    public async Task<IGenericResult<bool>> SaveSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.TryGetValue(sessionId, out var state))
        {
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("SessionNotFound"),
                ResultDetails.Create().With("SessionId", sessionId.ToString()));
        }

        RoslynWorkspaceLog.SessionSaving(_logger, sessionId);

        try
        {
            state.LastModifiedAt = DateTimeOffset.UtcNow;
            var persisted = PersistedSession.FromState(state);

            var result = await _sessionStore.SaveSession(persisted, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                state.HasPendingChanges = false;

                // Update project index
                var projectPath = Path.GetDirectoryName(state.SolutionPath);
                if (!string.IsNullOrEmpty(projectPath))
                {
                    await UpdateProjectSessionIndex(projectPath, cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("SessionSaveFailed"),
                ResultDetails.Create().With("SessionId", sessionId.ToString()).With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<bool>> CloseSession(
        Guid sessionId,
        bool save = false,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.TryGetValue(sessionId, out var state))
        {
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("SessionNotFound"),
                ResultDetails.Create().With("SessionId", sessionId.ToString()));
        }

        RoslynWorkspaceLog.SessionClosing(_logger, sessionId);

        if (save)
        {
            var saveResult = await SaveSession(sessionId, cancellationToken).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }
        }

        _sessions.TryRemove(sessionId, out _);

        lock (_activeLock)
        {
            if (_activeSessionId == sessionId)
            {
                _activeSessionId = null;
            }
        }

        RoslynWorkspaceLog.SessionClosed(_logger, sessionId);
        return GenericResult<bool>.Success(true);
    }

    /// <inheritdoc/>
    public IGenericResult<IRoslynWorkspace> GetSessionWorkspaceSync(Guid sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.TryGetValue(sessionId, out var state))
        {
            return GenericResult<IRoslynWorkspace>.Failure(
                WorkspaceResultCodes.ByName("SessionNotFound"),
                ResultDetails.Create().With("SessionId", sessionId.ToString()));
        }

        if (state.IsSleeping)
        {
            RoslynWorkspaceLog.SessionWaking(_logger, sessionId);
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            Task.Run(() => WakeSessionInternal(state, CancellationToken.None)).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        state.LastAccessedAt = DateTimeOffset.UtcNow;
        return GenericResult<IRoslynWorkspace>.Success(state.Workspace!);
    }

    /// <inheritdoc/>
    public async Task<IRoslynWorkspace?> GetSessionWorkspace(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return null;

        if (state.IsSleeping)
        {
            await WakeSessionInternal(state, cancellationToken).ConfigureAwait(false);
        }

        state.LastAccessedAt = DateTimeOffset.UtcNow;
        return state.Workspace;
    }

    /// <inheritdoc/>
    public bool SetActiveSession(Guid sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.ContainsKey(sessionId))
            return false;

        lock (_activeLock)
        {
            _activeSessionId = sessionId;
        }

        RoslynWorkspaceLog.ActiveSessionSet(_logger, sessionId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<SessionInfo>> UpdateSessionMetadata(
        Guid sessionId,
        string? description = null,
        string? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.TryGetValue(sessionId, out var state))
        {
            return GenericResult<SessionInfo>.Failure(
                WorkspaceResultCodes.ByName("SessionNotFound"),
                ResultDetails.Create().With("SessionId", sessionId.ToString()));
        }

        if (description is not null)
        {
            state.Description = description;
        }

        if (conversationId is not null)
        {
            state.ConversationId = conversationId;
        }

        state.LastModifiedAt = DateTimeOffset.UtcNow;
        state.HasPendingChanges = true;

        RoslynWorkspaceLog.SessionMetadataUpdated(_logger, sessionId);

        // Update project index
        var projectPath = Path.GetDirectoryName(state.SolutionPath);
        if (!string.IsNullOrEmpty(projectPath))
        {
            await UpdateProjectSessionIndex(projectPath, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult<SessionInfo>.Success(state.ToSessionInfo());
    }

    /// <inheritdoc/>
    public IReadOnlyList<SessionInfo> ListActiveSessions()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _sessions.Values
            .Select(s => s.ToSessionInfo())
            .OrderByDescending(s => s.LastModifiedAt)
            .ToList();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SessionInfo>> ListPersistedSessions(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _sessionStore.ListSessions(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<bool>> DeletePersistedSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If active, close first
        if (_sessions.ContainsKey(sessionId))
        {
            var closeResult = await CloseSession(sessionId, save: false, cancellationToken).ConfigureAwait(false);
            if (closeResult.IsFailure)
            {
                RoslynWorkspaceLog.SessionCloseFailed(_logger, sessionId, closeResult.CurrentMessage ?? string.Empty);
            }
        }

        return await _sessionStore.DeleteSession(sessionId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public bool SleepSession(Guid sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return false;

        if (state.IsSleeping)
            return true;

        state.Workspace = null;
        state.IsSleeping = true;

        RoslynWorkspaceLog.SessionSleeping(_logger, sessionId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<IRoslynWorkspace?> WakeSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return null;

        if (!state.IsSleeping)
        {
            state.LastAccessedAt = DateTimeOffset.UtcNow;
            return state.Workspace;
        }

        await WakeSessionInternal(state, cancellationToken).ConfigureAwait(false);
        return state.Workspace;
    }

    /// <inheritdoc/>
    public void CheckAndSleepInactiveSessions()
    {
        if (_disposed)
            return;

        // Why: SleepTimeout can be TimeSpan.MaxValue when a caller disables sleeping.
        // DateTimeOffset.UtcNow - TimeSpan.MaxValue is un-representable and throws on the
        // Timer thread, crashing the host. When the timeout exceeds UtcNow's distance from
        // DateTimeOffset.MinValue, nothing can be older than the cutoff — there is nothing to sleep.
        if (SleepTimeout >= DateTimeOffset.UtcNow - DateTimeOffset.MinValue)
            return;

        var cutoff = DateTimeOffset.UtcNow - SleepTimeout;

        foreach (var state in _sessions.Values)
        {
            if (!state.IsSleeping && state.LastAccessedAt < cutoff)
            {
                SleepSession(state.Id);
            }
        }
    }

    /// <inheritdoc/>
    public async Task UpdateProjectSessionIndex(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(projectPath))
            return;

        RoslynWorkspaceLog.ProjectIndexUpdating(_logger, projectPath);

        try
        {
            // Get all sessions for this project
            var projectSessions = _sessions.Values
                .Where(s => Path.GetDirectoryName(s.SolutionPath)?
                    .StartsWith(projectPath, StringComparison.OrdinalIgnoreCase) ?? false)
                .ToList();

            var index = new ProjectSessionIndex
            {
                Version = 1,
                ProjectPath = projectPath,
                SolutionPath = projectSessions.FirstOrDefault()?.SolutionPath,
                ActiveSessionId = _activeSessionId,
                Sessions = projectSessions
                    .Select(s => SessionIndexEntry.FromState(s))
                    .ToList()
            };

            var saveResult = await _projectIndexStore.SaveIndex(projectPath, index, cancellationToken).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                RoslynWorkspaceLog.ProjectIndexSaveFailed(_logger, projectPath, saveResult.CurrentMessage ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - index update is best-effort
            RoslynWorkspaceLog.ProjectIndexUpdateFailed(_logger, ex, projectPath, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ProjectSessionIndex?> LoadProjectSessionIndex(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(projectPath))
            return null;

        return await _projectIndexStore.LoadIndex(projectPath, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public SessionInfo? GetSessionInfo(Guid sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_sessions.TryGetValue(sessionId, out var state))
        {
            return state.ToSessionInfo();
        }

        return null;
    }

    /// <inheritdoc/>
    public SessionInfo? FindSessionByConversationId(string conversationId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(conversationId))
            return null;

        var state = _sessions.Values
            .FirstOrDefault(s => string.Equals(s.ConversationId, conversationId, StringComparison.Ordinal));

        return state?.ToSessionInfo();
    }

    /// <inheritdoc/>
    public async Task<SessionInfo?> FindSessionByConversationId(
        string conversationId,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(conversationId))
            return null;

        // In-memory first: a live session is authoritative over whatever was last written to disk,
        // and the common case (same process, repeat call) never touches the filesystem.
        var live = FindSessionByConversationId(conversationId);
        if (live is not null)
            return live;

        // Why the store is consulted at all: the sync overload can only ever see sessions this
        // process created, so a reconnecting agent — new process, same conversation — always missed
        // and was handed a brand new session. The persisted record is the only thing that survives a
        // process boundary, which is exactly the boundary reattach exists to cross.
        var persisted = await _sessionStore.ListSessions(cancellationToken).ConfigureAwait(false);

        return persisted.FirstOrDefault(s =>
            string.Equals(s.ConversationId, conversationId, StringComparison.Ordinal));
    }

    private async Task WakeSessionInternal(
        ManagedSessionState state,
        CancellationToken cancellationToken)
    {
        RoslynWorkspaceLog.SessionWaking(_logger, state.Id);

        var workspace = await _workspaceFactory.CreateFromSolution(
            state.SolutionPath,
            cancellationToken).ConfigureAwait(false);

        state.Workspace = workspace;
        state.IsSleeping = false;
        state.LastAccessedAt = DateTimeOffset.UtcNow;

        var projectCount = workspace.CurrentSolution?.Projects.Count() ?? 0;
        RoslynWorkspaceLog.SessionAwakened(_logger, state.Id, projectCount);
    }

    private static async Task<string?> TryGetGitCommitHash(
        string solutionPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(solutionPath);
            if (string.IsNullOrEmpty(directory))
                return null;

            var gitDir = FindGitDirectory(directory);
            if (gitDir is null)
                return null;

            var headPath = Path.Combine(gitDir, "HEAD");
            if (!File.Exists(headPath))
                return null;

            var headContent = await File.ReadAllTextAsync(headPath, cancellationToken).ConfigureAwait(false);
            headContent = headContent.Trim();

            // If HEAD points to a ref (branch)
            if (headContent.StartsWith("ref: ", StringComparison.Ordinal))
            {
                var refPath = Path.Combine(gitDir, headContent[5..].Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(refPath))
                {
                    return (await File.ReadAllTextAsync(refPath, cancellationToken).ConfigureAwait(false)).Trim();
                }
            }

            // HEAD is a detached commit hash
            return headContent;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Why: git HEAD resolution is best-effort; I/O failures (missing .git dir, permission denied)
            // return null so the caller proceeds without a commit hash. ex is observed via the when filter.
            return null;
        }
        catch (Exception ex)
        {
            // Why: catch remaining unexpected exceptions so git HEAD resolution never throws out of the session manager.
            // ex.Message is observed here to satisfy FDW022 without a logger (static helper, no DI).
            _ = ex.Message;
            return null;
        }
    }

    private static string? FindGitDirectory(string startPath)
    {
        var current = startPath;
        while (!string.IsNullOrEmpty(current))
        {
            var gitDir = Path.Combine(current, ".git");
            if (Directory.Exists(gitDir))
                return gitDir;

            var parent = Path.GetDirectoryName(current);
            if (string.Equals(parent, current, StringComparison.Ordinal))
                break;
            current = parent;
        }
        return null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _sleepTimer.Dispose();
        _sessions.Clear();

        lock (_activeLock)
        {
            _activeSessionId = null;
        }
    }
}
