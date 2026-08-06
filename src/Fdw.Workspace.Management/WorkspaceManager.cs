using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Management.Logging;
using Fdw.Workspace.Roslyn;
using Fdw.Workspace.Roslyn.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Workspace.Management;

/// <summary>
/// Default implementation of <see cref="IWorkspaceManager"/> that manages multiple
/// Roslyn workspaces with session persistence support.
/// </summary>
public sealed class WorkspaceManager : IWorkspaceManager
{
    private readonly ConcurrentDictionary<Guid, ManagedWorkspace> _workspaces = new();
    private readonly IWorkspaceSessionStore _sessionStore;
    private readonly IRoslynWorkspaceFactory _workspaceFactory;
    private readonly ILogger<WorkspaceManager> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceManager"/> class.
    /// </summary>
    /// <param name="sessionStore">The session store for persistence.</param>
    /// <param name="workspaceFactory">The factory for creating workspaces.</param>
    /// <param name="logger">Optional logger.</param>
    public WorkspaceManager(
        IWorkspaceSessionStore sessionStore,
        IRoslynWorkspaceFactory? workspaceFactory = null,
        ILogger<WorkspaceManager>? logger = null)
    {
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _workspaceFactory = workspaceFactory ?? new RoslynWorkspaceFactory();
        _logger = logger ?? NullLogger<WorkspaceManager>.Instance;
    }

    /// <inheritdoc/>
    public int WorkspaceCount => _workspaces.Count;

    /// <inheritdoc/>
    public async Task<IGenericResult<Guid>> LoadWorkspace(string solutionPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return GenericResult<Guid>.Failure(WorkspaceResultCodes.ByName("SolutionPathRequired"));

        if (!File.Exists(solutionPath))
            return GenericResult<Guid>.Failure(
                WorkspaceResultCodes.ByName("SolutionFileNotFound"),
                ResultDetails.Create("SolutionPath", solutionPath));

        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var workspaceId = Guid.NewGuid();

            WorkspaceManagementLog.WorkspaceLoading(_logger, solutionPath);

            var workspace = await _workspaceFactory.CreateFromSolution(solutionPath, cancellationToken).ConfigureAwait(false);

            var managed = new ManagedWorkspace
            {
                Id = workspaceId,
                Workspace = workspace,
                SolutionPath = solutionPath,
                Name = Path.GetFileNameWithoutExtension(solutionPath),
                LoadedAt = DateTimeOffset.UtcNow
            };

            if (!_workspaces.TryAdd(workspaceId, managed))
            {
                (workspace as IDisposable)?.Dispose();
                return GenericResult<Guid>.Failure(WorkspaceResultCodes.ByName("WorkspaceRegistrationFailed"));
            }

            WorkspaceManagementLog.WorkspaceLoaded(_logger, workspaceId, managed.Name, workspace.CurrentSolution.ProjectIds.Count);

            return GenericResult<Guid>.Success(workspaceId);
        }
        catch (Exception ex)
        {
            WorkspaceManagementLog.WorkspaceLoadFailed(_logger, ex, solutionPath);
            return GenericResult<Guid>.Failure(
                WorkspaceResultCodes.ByName("WorkspaceLoadFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message));
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IRoslynWorkspace>> GetWorkspace(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        if (_workspaces.TryGetValue(workspaceId, out var managed))
        {
            return Task.FromResult(GenericResult<IRoslynWorkspace>.Success(managed.Workspace));
        }

        return Task.FromResult(GenericResult<IRoslynWorkspace>.Failure(
            WorkspaceResultCodes.ByName("WorkspaceNotFound"),
            ResultDetails.Create("WorkspaceId", workspaceId)));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<bool>> UnloadWorkspace(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        if (_workspaces.TryRemove(workspaceId, out var managed))
        {
            WorkspaceManagementLog.WorkspaceUnloading(_logger, workspaceId, managed.Name);

            (managed.Workspace as IDisposable)?.Dispose();
            return Task.FromResult(GenericResult<bool>.Success(true));
        }

        return Task.FromResult(GenericResult<bool>.Failure(
            WorkspaceResultCodes.ByName("WorkspaceNotFound"),
            ResultDetails.Create("WorkspaceId", workspaceId)));
    }

    /// <inheritdoc/>
    // MA0051: Method length acceptable - session save orchestration (capture snapshots, build session object, persist)
#pragma warning disable MA0051 // Method is too long
    public async Task<IGenericResult<Guid>> SaveSession(Guid workspaceId, CancellationToken cancellationToken = default)
#pragma warning restore MA0051
    {
        if (!_workspaces.TryGetValue(workspaceId, out var managed))
            return GenericResult<Guid>.Failure(
                WorkspaceResultCodes.ByName("WorkspaceNotFound"),
                ResultDetails.Create("WorkspaceId", workspaceId));

        var workspace = managed.Workspace;
        var sessionId = Guid.NewGuid();

        // Capture all snapshots from the workspace
        var snapshotRecords = new List<SnapshotRecord>();
        foreach (var snapshotInfo in workspace.ListSnapshots())
        {
            var changes = workspace.GetChangesFromSnapshot(snapshotInfo.Id);
            if (changes is not null)
            {
                snapshotRecords.Add(new SnapshotRecord
                {
                    Id = snapshotInfo.Id,
                    Name = snapshotInfo.Name,
                    Description = snapshotInfo.Description,
                    CreatedAt = new DateTimeOffset(snapshotInfo.CreatedAt, TimeSpan.Zero),
                    DocumentChanges = new Dictionary<string, string>(changes, StringComparer.Ordinal)
                });
            }
        }

        // Capture baseline changes (changes from disk state)
        var baselineChanges = workspace.GetChangesFromBaseline();

        var session = new WorkspaceSession
        {
            Id = sessionId,
            WorkspaceId = workspaceId,
            SolutionPath = managed.SolutionPath,
            Name = managed.Name,
            CreatedAt = managed.LoadedAt,
            SavedAt = DateTimeOffset.UtcNow,
            Version = 1,
            Snapshots = snapshotRecords,
            BaselineSnapshot = workspace.Baseline is not null ? "baseline" : null
        };

        // Store baseline changes as a special snapshot if there are any
        if (baselineChanges.Count > 0)
        {
            session.Snapshots.Insert(0, new SnapshotRecord
            {
                Id = "baseline",
                Name = "Baseline",
                Description = "Changes from disk state",
                CreatedAt = managed.LoadedAt,
                DocumentChanges = new Dictionary<string, string>(baselineChanges, StringComparer.Ordinal)
            });
        }

        var result = await _sessionStore.Save(session, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return GenericResult<Guid>.Failure(
                WorkspaceResultCodes.ByName("SessionSaveFailed"),
                ResultDetails.Create("ErrorMessage", result.CurrentMessage));

        WorkspaceManagementLog.SessionSaved(_logger, sessionId, workspaceId, managed.Name, session.Snapshots.Count);

        return GenericResult<Guid>.Success(sessionId);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<Guid>> ResumeSession(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var sessionResult = await _sessionStore.Load(sessionId, cancellationToken).ConfigureAwait(false);
        if (!sessionResult.IsSuccess)
            return GenericResult<Guid>.Failure(
                WorkspaceResultCodes.ByName("SessionLoadFailed"),
                ResultDetails.Create("ErrorMessage", sessionResult.CurrentMessage));

        var session = sessionResult.Value!;

        if (!File.Exists(session.SolutionPath))
            return GenericResult<Guid>.Failure(
                WorkspaceResultCodes.ByName("SolutionFileNotFound"),
                ResultDetails.Create("SolutionPath", session.SolutionPath));

        WorkspaceManagementLog.SessionResuming(_logger, sessionId, session.Name, session.SolutionPath);

        // Load the workspace from the solution
        var loadResult = await LoadWorkspace(session.SolutionPath, cancellationToken).ConfigureAwait(false);
        if (!loadResult.IsSuccess)
            return loadResult;

        var workspaceId = loadResult.Value;
        if (!_workspaces.TryGetValue(workspaceId, out var managed))
            return GenericResult<Guid>.Failure(WorkspaceResultCodes.ByName("WorkspaceRetrievalFailed"));

        var workspace = managed.Workspace;

        // Apply baseline changes first (changes from disk state when session was saved)
        var baselineRecord = session.Snapshots.FirstOrDefault(s => string.Equals(s.Id, "baseline", StringComparison.Ordinal));
        if (baselineRecord is not null && baselineRecord.DocumentChanges.Count > 0)
        {
            workspace.ApplyDocumentChanges(
                new Dictionary<string, string>(baselineRecord.DocumentChanges, StringComparer.Ordinal));

            WorkspaceManagementLog.BaselineChangesApplied(_logger, baselineRecord.DocumentChanges.Count);
        }

        // Recreate snapshots (excluding baseline which we already applied)
        foreach (var snapshotRecord in session.Snapshots.Where(s => !string.Equals(s.Id, "baseline", StringComparison.Ordinal)))
        {
            // Create the snapshot with the stored name and description
            var newSnapshotId = workspace.CreateSnapshot(snapshotRecord.Name, snapshotRecord.Description ?? string.Empty);

            WorkspaceManagementLog.SnapshotRecreated(_logger, snapshotRecord.Name, newSnapshotId);
        }

        WorkspaceManagementLog.SessionResumedWithSnapshots(
            _logger,
            sessionId,
            workspaceId,
            session.Snapshots.Count(s => !string.Equals(s.Id, "baseline", StringComparison.Ordinal)));

        return GenericResult<Guid>.Success(workspaceId);
    }

    /// <inheritdoc/>
    public IEnumerable<WorkspaceInfo> ListWorkspaces()
    {
        return _workspaces.Values.Select(m => new WorkspaceInfo
        {
            Id = m.Id,
            SolutionPath = m.SolutionPath,
            Name = m.Name,
            ProjectCount = m.Workspace.CurrentSolution.ProjectIds.Count,
            LoadedAt = m.LoadedAt,
            HasChanges = m.Workspace.HasChanges,
            SnapshotCount = m.Workspace.SnapshotCount,
            HasBaseline = m.Workspace.Baseline is not null
        });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SessionInfo>> ListSessions(CancellationToken cancellationToken = default)
    {
        return _sessionStore.List(cancellationToken);
    }

    /// <inheritdoc/>
    public bool IsLoaded(Guid workspaceId) => _workspaces.ContainsKey(workspaceId);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var managed in _workspaces.Values)
        {
            (managed.Workspace as IDisposable)?.Dispose();
        }

        _workspaces.Clear();
        _loadLock.Dispose();
        _disposed = true;
    }

    private sealed class ManagedWorkspace
    {
        public Guid Id { get; init; }
        public IRoslynWorkspace Workspace { get; init; } = null!;
        public string SolutionPath { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public DateTimeOffset LoadedAt { get; init; }
    }
}
