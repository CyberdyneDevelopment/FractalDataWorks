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
/// Manages multiple Roslyn workspaces with lifecycle management including
/// caching, sleep/wake functionality, and activity tracking.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class WorkspaceManager : IWorkspaceManager
{
    private readonly IRoslynWorkspaceFactory _workspaceFactory;
    private readonly ILogger<WorkspaceManager> _logger;
    private readonly ConcurrentDictionary<string, ManagedWorkspaceState> _workspaces = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _pathToId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer _sleepTimer;
    private readonly Lock _activeLock = new();

    private string? _activeWorkspaceId;
    private bool _disposed;

    /// <summary>
    /// Default sleep timeout of 5 minutes.
    /// </summary>
    public static readonly TimeSpan DefaultSleepTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceManager"/> class.
    /// </summary>
    /// <param name="workspaceFactory">Factory for creating workspaces.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="sleepTimeout">Optional sleep timeout. Defaults to 5 minutes.</param>
    public WorkspaceManager(
        IRoslynWorkspaceFactory workspaceFactory,
        ILogger<WorkspaceManager>? logger = null,
        TimeSpan? sleepTimeout = null)
    {
        _workspaceFactory = workspaceFactory ?? throw new ArgumentNullException(nameof(workspaceFactory));
        _logger = logger ?? NullLogger<WorkspaceManager>.Instance;
        SleepTimeout = sleepTimeout ?? DefaultSleepTimeout;

        // Start the sleep check timer - runs every minute
        _sleepTimer = new Timer(
            _ => CheckAndSleepInactiveWorkspaces(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc/>
    public TimeSpan SleepTimeout { get; }

    /// <inheritdoc/>
    public IRoslynWorkspace? ActiveWorkspace
    {
        get
        {
            lock (_activeLock)
            {
                if (_activeWorkspaceId is null)
                    return null;

                if (_workspaces.TryGetValue(_activeWorkspaceId, out var state))
                {
                    state.LastAccessedAt = DateTime.UtcNow;
                    return state.Workspace;
                }

                return null;
            }
        }
    }

    /// <inheritdoc/>
    public string? ActiveWorkspaceId
    {
        get
        {
            lock (_activeLock)
            {
                return _activeWorkspaceId;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<(string Id, IRoslynWorkspace Workspace)> OpenSolution(
        string solutionPath,
        bool setAsActive = true,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(solutionPath))
            throw new ArgumentNullException(nameof(solutionPath));

        // Normalize the path
        var fullPath = Path.GetFullPath(solutionPath);

        // Check if already open
        if (_pathToId.TryGetValue(fullPath, out var existingId))
        {
            if (_workspaces.TryGetValue(existingId, out var existingState))
            {
                RoslynWorkspaceLog.SolutionAlreadyOpen(_logger, fullPath);

                // Wake if sleeping — under the same gate as every other wake path. A second
                // load_solution for an already-open path is one of the ways two callers ended up
                // reloading one workspace at the same time.
                await existingState.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (existingState.IsSleeping)
                    {
                        await WakeWorkspaceInternal(existingState, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    existingState.Gate.Release();
                }

                existingState.LastAccessedAt = DateTime.UtcNow;

                if (setAsActive)
                {
                    SetActiveWorkspace(existingId);
                }

                return (existingId, existingState.Workspace!);
            }
        }

        // Open new solution
        RoslynWorkspaceLog.SolutionOpening(_logger, fullPath);
        var workspace = await _workspaceFactory.CreateFromSolution(fullPath, cancellationToken).ConfigureAwait(false);

        var id = Guid.NewGuid().ToString("N");
        var state = new ManagedWorkspaceState
        {
            Id = id,
            SolutionPath = fullPath,
            Workspace = workspace,
            ProjectCount = workspace.CurrentSolution.Projects.Count(),
            LoadedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };

        _workspaces[id] = state;
        _pathToId[fullPath] = id;

        RoslynWorkspaceLog.SolutionOpenedWithId(_logger, fullPath, state.ProjectCount, id);

        if (setAsActive)
        {
            SetActiveWorkspace(id);
        }

        return (id, workspace);
    }

    /// <inheritdoc/>
    public async Task<IRoslynWorkspace?> GetWorkspace(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(workspaceId))
            return null;

        if (!_workspaces.TryGetValue(workspaceId, out var state))
            return null;

        await state.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (state.IsSleeping)
            {
                await WakeWorkspaceInternal(state, cancellationToken).ConfigureAwait(false);
            }

            state.LastAccessedAt = DateTime.UtcNow;
            return state.Workspace;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    /// <inheritdoc/>
    public IGenericResult<IRoslynWorkspace> GetWorkspaceSync(string workspaceId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(workspaceId))
        {
            return GenericResult<IRoslynWorkspace>.Failure(
                WorkspaceResultCodes.ByName("NoSolutionLoaded"));
        }

        if (!_workspaces.TryGetValue(workspaceId, out var state))
        {
            return GenericResult<IRoslynWorkspace>.Failure(
                WorkspaceResultCodes.ByName("WorkspaceNotFound"),
                ResultDetails.Create().With("WorkspaceId", workspaceId));
        }

        // Why: the same gate as the async paths, taken inside the Task.Run so the blocking wait happens
        // on the thread pool rather than on the caller's thread. Without it a sync caller and an async
        // caller could wake the same workspace simultaneously, which is the identical race.
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits — documented sync-over-async.
        Task.Run(async () =>
        {
            await state.Gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                if (state.IsSleeping)
                {
                    await WakeWorkspaceInternal(state, CancellationToken.None).ConfigureAwait(false);
                }
            }
            finally
            {
                state.Gate.Release();
            }
        }).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

        state.LastAccessedAt = DateTime.UtcNow;
        return GenericResult<IRoslynWorkspace>.Success(state.Workspace!);
    }

    /// <inheritdoc/>
    public async Task<IRoslynWorkspace?> GetWorkspaceByPath(
        string solutionPath,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(solutionPath))
            return null;

        var fullPath = Path.GetFullPath(solutionPath);

        if (!_pathToId.TryGetValue(fullPath, out var id))
            return null;

        return await GetWorkspace(id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public bool SetActiveWorkspace(string workspaceId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(workspaceId))
            return false;

        if (!_workspaces.ContainsKey(workspaceId))
            return false;

        lock (_activeLock)
        {
            _activeWorkspaceId = workspaceId;
        }

        RoslynWorkspaceLog.ActiveWorkspaceSet(_logger, workspaceId);
        return true;
    }

    /// <inheritdoc/>
    public bool CloseWorkspace(string workspaceId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(workspaceId))
            return false;

        if (!_workspaces.TryRemove(workspaceId, out var state))
            return false;

        _pathToId.TryRemove(state.SolutionPath, out _);

        lock (_activeLock)
        {
            if (string.Equals(_activeWorkspaceId, workspaceId, StringComparison.Ordinal))
            {
                _activeWorkspaceId = null;
            }
        }

        RoslynWorkspaceLog.WorkspaceClosed(_logger, state.SolutionPath, workspaceId);
        return true;
    }

    /// <inheritdoc/>
    public void CloseAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var ids = _workspaces.Keys.ToList();
        foreach (var id in ids)
        {
            CloseWorkspace(id);
        }

        RoslynWorkspaceLog.AllWorkspacesClosed(_logger);
    }

    /// <inheritdoc/>
    public IReadOnlyList<ManagedWorkspaceInfo> ListWorkspaces()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _workspaces.Values
            .Select(s => new ManagedWorkspaceInfo(
                s.Id,
                s.SolutionPath,
                s.ProjectCount,
                !s.IsSleeping,
                s.LastAccessedAt,
                s.LoadedAt))
            .OrderByDescending(w => w.LastAccessedAt)
            .ToList();
    }

    /// <inheritdoc/>
    public bool SleepWorkspace(string workspaceId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(workspaceId))
            return false;

        if (!_workspaces.TryGetValue(workspaceId, out var state))
            return false;

        // Why: TimeSpan.Zero, so the Timer thread never blocks behind an in-flight wake. A workspace
        // being woken right now is by definition not idle, and the next tick will reconsider it.
        if (!state.Gate.Wait(TimeSpan.Zero))
        {
            RoslynWorkspaceLog.WorkspaceSleepSkippedBusy(_logger, workspaceId);
            return false;
        }

        try
        {
            if (state.IsSleeping)
                return true; // Already sleeping

            // Why: eviction discards the in-memory Solution, and pending edits live nowhere else. A
            // refactor followed by an ApplyWorkspaceChanges across an idle tick used to return
            // success: true / "Wrote 0 file(s) to disk" with the work silently gone — two successes and
            // no effect. Memory reclamation is not a reason to destroy uncommitted work; a workspace
            // holding changes stays resident until they are written or explicitly discarded.
            var pending = state.Workspace!.GetChangesFromBaseline().Count;
            if (pending > 0)
            {
                RoslynWorkspaceLog.WorkspaceSleepRefusedPendingChanges(_logger, workspaceId, pending);
                return false;
            }

            state.Workspace = null;
            RoslynWorkspaceLog.WorkspaceSleeping(_logger, workspaceId, state.SolutionPath);
            return true;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IRoslynWorkspace?> WakeWorkspace(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(workspaceId))
            return null;

        if (!_workspaces.TryGetValue(workspaceId, out var state))
            return null;

        // Why: the re-check inside the gate is the whole fix. Three callers racing one sleeping
        // workspace each saw IsSleeping==true, each reloaded the solution, and the last assignment won —
        // leaving two orphaned IRoslynWorkspace instances for a single id and discarding anything the
        // losers had already been handed.
        await state.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!state.IsSleeping)
            {
                state.LastAccessedAt = DateTime.UtcNow;
                return state.Workspace;
            }

            await WakeWorkspaceInternal(state, cancellationToken).ConfigureAwait(false);
            return state.Workspace;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    /// <inheritdoc/>
    public void CheckAndSleepInactiveWorkspaces()
    {
        if (_disposed)
            return;

        // Why: SleepTimeout can be TimeSpan.MaxValue when a caller disables sleeping
        // (the stdio MCP host does exactly this). DateTime.UtcNow - TimeSpan.MaxValue is
        // un-representable and throws ArgumentOutOfRangeException; because this runs on a
        // Timer thread with no caller to observe it, the unhandled exception tears down
        // the whole host. When the timeout exceeds UtcNow's distance from DateTime.MinValue,
        // no workspace can ever be older than the cutoff, so there is nothing to sleep.
        if (SleepTimeout >= DateTime.UtcNow - DateTime.MinValue)
            return;

        var cutoff = DateTime.UtcNow - SleepTimeout;

        foreach (var state in _workspaces.Values)
        {
            if (!state.IsSleeping && state.LastAccessedAt < cutoff)
            {
                SleepWorkspace(state.Id);
            }
        }
    }

    private async Task WakeWorkspaceInternal(
        ManagedWorkspaceState state,
        CancellationToken cancellationToken)
    {
        RoslynWorkspaceLog.WorkspaceWaking(_logger, state.Id);

        var workspace = await _workspaceFactory.CreateFromSolution(
            state.SolutionPath,
            cancellationToken).ConfigureAwait(false);

        state.Workspace = workspace;
        state.ProjectCount = workspace.CurrentSolution.Projects.Count();
        state.LastAccessedAt = DateTime.UtcNow;

        RoslynWorkspaceLog.WorkspaceAwakened(_logger, state.Id, state.ProjectCount);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _sleepTimer.Dispose();
        _workspaces.Clear();
        _pathToId.Clear();

        lock (_activeLock)
        {
            _activeWorkspaceId = null;
        }
    }
}
