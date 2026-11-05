using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Roslyn.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// A proxy implementation of <see cref="IRoslynWorkspace"/> that delegates to the active workspace
/// from the workspace manager. This allows singleton tools to always use the current active workspace.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class ActiveWorkspaceProxy : IRoslynWorkspace
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly ILogger<ActiveWorkspaceProxy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveWorkspaceProxy"/> class.
    /// </summary>
    public ActiveWorkspaceProxy(IWorkspaceManager workspaceManager, ILogger<ActiveWorkspaceProxy>? logger = null)
    {
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _logger = logger ?? NullLogger<ActiveWorkspaceProxy>.Instance;
    }

    /// <summary>
    /// Gets the active workspace as a result, allowing callers to handle failures gracefully.
    /// </summary>
    /// <returns>A result containing the workspace, or a failure message if no workspace is active.</returns>
    public IGenericResult<IRoslynWorkspace> GetWorkspaceResult()
    {
        var activeId = _workspaceManager.ActiveWorkspaceId;
        if (activeId is null)
            return GenericResult<IRoslynWorkspace>.Failure(WorkspaceLogger.NoSolutionLoaded(_logger));

        return _workspaceManager.GetWorkspaceSync(activeId);
    }

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
    public int SnapshotCount => GetWorkspace().SnapshotCount;

    /// <inheritdoc/>
    public bool HasChanges => GetWorkspace().HasChanges;

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
        GetWorkspace().ListSnapshots();

    /// <inheritdoc/>
    public bool RemoveSnapshot(string snapshotId) =>
        GetWorkspace().RemoveSnapshot(snapshotId);

    /// <inheritdoc/>
    public void ClearSnapshots() => GetWorkspace().ClearSnapshots();

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> GetChangesFromBaseline() =>
        GetWorkspace().GetChangesFromBaseline();

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string>? GetChangesFromSnapshot(string snapshotId) =>
        GetWorkspace().GetChangesFromSnapshot(snapshotId);

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
        GetWorkspace().GetAllProjects();

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetLoadedProjects() =>
        GetWorkspace().GetLoadedProjects();

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetExcludedProjects() =>
        GetWorkspace().GetExcludedProjects();

    /// <inheritdoc/>
    public IReadOnlyList<string> ExcludePatterns =>
        GetWorkspace().ExcludePatterns;

    /// <inheritdoc/>
    public IReadOnlyList<string> LoadDiagnostics =>
        GetWorkspace().LoadDiagnostics;

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
        GetWorkspace().HasPendingChanges(projectName);

    /// <inheritdoc/>
    public void SetExcludePatterns(IReadOnlyList<string> patterns) =>
        GetWorkspace().SetExcludePatterns(patterns);
}
