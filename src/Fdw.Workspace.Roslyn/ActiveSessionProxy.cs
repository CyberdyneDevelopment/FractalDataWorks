using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Roslyn.Results;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// A proxy implementation of <see cref="IRoslynWorkspace"/> that delegates to the active session's
/// workspace from the session manager. This allows singleton tools to always use the current active session.
/// </summary>
/// <remarks>
/// <para>
/// This proxy is the recommended way to inject an <see cref="IRoslynWorkspace"/> into MCP tools
/// when using session-based workspace management. It automatically resolves to the workspace
/// of the currently active session.
/// </para>
/// <para>
/// When no session is active, operations return empty results or delegate to
/// <see cref="NullRoslynWorkspace.Instance"/>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class ActiveSessionProxy : IRoslynWorkspace
{
    private readonly IRoslynSessionManager _sessionManager;
    private readonly ILogger<ActiveSessionProxy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveSessionProxy"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager providing workspace access.</param>
    /// <param name="logger">Optional logger for diagnostic messages.</param>
    public ActiveSessionProxy(
        IRoslynSessionManager sessionManager,
        ILogger<ActiveSessionProxy>? logger = null)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _logger = logger ?? NullLogger<ActiveSessionProxy>.Instance;
    }

    /// <summary>
    /// Gets the active session's workspace as a result, allowing callers to handle failures gracefully.
    /// </summary>
    /// <returns>A result containing the workspace, or a failure message if no session is active.</returns>
    public IGenericResult<IRoslynWorkspace> GetWorkspaceResult()
    {
        var activeId = _sessionManager.ActiveSessionId;
        if (activeId is null)
        {
            return GenericResult<IRoslynWorkspace>.Failure(
                WorkspaceResultCodes.ByName("NoActiveSession"));
        }

        return _sessionManager.GetSessionWorkspaceSync(activeId.Value);
    }

    /// <summary>
    /// Gets information about the active session.
    /// </summary>
    /// <returns>The active session info, or null if no session is active.</returns>
    public SessionInfo? GetActiveSessionInfo()
    {
        var activeId = _sessionManager.ActiveSessionId;
        return activeId.HasValue ? _sessionManager.GetSessionInfo(activeId.Value) : null;
    }

    /// <summary>
    /// Gets the active session ID.
    /// </summary>
    public Guid? ActiveSessionId => _sessionManager.ActiveSessionId;

    /// <summary>
    /// Checks if there is an active session.
    /// </summary>
    public bool HasActiveSession => _sessionManager.ActiveSessionId.HasValue;

    private IRoslynWorkspace GetWorkspace()
    {
        var result = GetWorkspaceResult();
        return result.IsSuccess ? result.Value! : NullRoslynWorkspace.Instance;
    }

    /// <inheritdoc/>
    public Solution CurrentSolution => GetWorkspace().CurrentSolution;

    /// <inheritdoc/>
    public Solution? BaselineSolution => GetWorkspace().BaselineSolution;

    /// <inheritdoc/>
    public Solution Current => GetWorkspace().Current;

    /// <inheritdoc/>
    public Solution? Baseline => GetWorkspace().Baseline;

    /// <inheritdoc/>
    public int SnapshotCount => _sessionManager.ActiveWorkspace?.SnapshotCount ?? 0;

    /// <inheritdoc/>
    public bool HasChanges => _sessionManager.ActiveWorkspace?.HasChanges ?? false;

    /// <inheritdoc/>
    public void UpdateSolution(Solution solution) => GetWorkspace().UpdateSolution(solution);

    /// <inheritdoc/>
    public void Update(Solution state) => GetWorkspace().Update(state);

    /// <inheritdoc/>
    public void SetBaseline(Solution state) => GetWorkspace().SetBaseline(state);

    /// <inheritdoc/>
    public string CreateSnapshot(string name, string description) =>
        GetWorkspace().CreateSnapshot(name, description);

    /// <inheritdoc/>
    public IGenericResult<Solution> RestoreSnapshot(string snapshotId) =>
        GetWorkspace().RestoreSnapshot(snapshotId);

    /// <inheritdoc/>
    public IEnumerable<SnapshotInfo> ListSnapshots() =>
        _sessionManager.ActiveWorkspace?.ListSnapshots() ?? [];

    /// <inheritdoc/>
    public bool RemoveSnapshot(string snapshotId) =>
        _sessionManager.ActiveWorkspace?.RemoveSnapshot(snapshotId) ?? false;

    /// <inheritdoc/>
    public void ClearSnapshots() => _sessionManager.ActiveWorkspace?.ClearSnapshots();

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> GetChangesFromBaseline() =>
        _sessionManager.ActiveWorkspace?.GetChangesFromBaseline()
        ?? new Dictionary<string, string>(StringComparer.Ordinal);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string>? GetChangesFromSnapshot(string snapshotId) =>
        _sessionManager.ActiveWorkspace?.GetChangesFromSnapshot(snapshotId);

    /// <inheritdoc/>
    public void ApplyDocumentChanges(IReadOnlyDictionary<string, string> documentChanges) =>
        GetWorkspace().ApplyDocumentChanges(documentChanges);

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(CancellationToken cancellationToken = default) =>
        GetWorkspace().ApplyChanges(cancellationToken);

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(
        bool deleteRemovedFiles,
        CancellationToken cancellationToken = default) =>
        GetWorkspace().ApplyChanges(deleteRemovedFiles, cancellationToken);

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetAllProjects() =>
        _sessionManager.ActiveWorkspace?.GetAllProjects() ?? [];

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetLoadedProjects() =>
        _sessionManager.ActiveWorkspace?.GetLoadedProjects() ?? [];

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetExcludedProjects() =>
        _sessionManager.ActiveWorkspace?.GetExcludedProjects() ?? [];

    /// <inheritdoc/>
    public IReadOnlyList<string> ExcludePatterns =>
        _sessionManager.ActiveWorkspace?.ExcludePatterns ?? [];

    /// <inheritdoc/>
    public IReadOnlyList<string> LoadDiagnostics =>
        _sessionManager.ActiveWorkspace?.LoadDiagnostics ?? [];

    /// <inheritdoc/>
    public Task<IGenericResult<ProjectInfo>> LoadProject(
        string projectName,
        CancellationToken cancellationToken = default) =>
        GetWorkspace().LoadProject(projectName, cancellationToken);

    /// <inheritdoc/>
    public IGenericResult<ProjectInfo> UnloadProject(string projectName, bool force = false) =>
        GetWorkspace().UnloadProject(projectName, force);

    /// <inheritdoc/>
    public bool HasPendingChanges(string projectName) =>
        _sessionManager.ActiveWorkspace?.HasPendingChanges(projectName) ?? false;

    /// <inheritdoc/>
    public void SetExcludePatterns(IReadOnlyList<string> patterns) =>
        _sessionManager.ActiveWorkspace?.SetExcludePatterns(patterns);
}
