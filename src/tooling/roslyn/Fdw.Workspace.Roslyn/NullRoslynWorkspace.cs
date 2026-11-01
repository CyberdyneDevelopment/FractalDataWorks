using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Roslyn.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// A null object implementation of <see cref="IRoslynWorkspace"/> that throws helpful errors when accessed.
/// </summary>
/// <remarks>
/// This allows the MCP server to start without a solution loaded. Tools will fail gracefully
/// at execution time with a clear message to use OpenSolution first.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class NullRoslynWorkspace : IRoslynWorkspace
{
    /// <summary>
    /// Gets the singleton instance of the null workspace.
    /// </summary>
    public static NullRoslynWorkspace Instance { get; } = new();

    private NullRoslynWorkspace() { }

    private static InvalidOperationException NoWorkspaceError() =>
        new("No solution is loaded. Use the OpenSolution tool to load a solution first.");

    /// <inheritdoc/>
    public Solution CurrentSolution => throw NoWorkspaceError();

    /// <inheritdoc/>
    public Solution? BaselineSolution => throw NoWorkspaceError();

    /// <inheritdoc/>
    public Solution Current => throw NoWorkspaceError();

    /// <inheritdoc/>
    public Solution? Baseline => throw NoWorkspaceError();

    /// <inheritdoc/>
    public int SnapshotCount => 0;

    /// <inheritdoc/>
    public bool HasChanges => false;

    /// <inheritdoc/>
    public void UpdateSolution(Solution solution) => throw NoWorkspaceError();

    /// <inheritdoc/>
    public void Update(Solution state) => throw NoWorkspaceError();

    /// <inheritdoc/>
    public void SetBaseline(Solution state) => throw NoWorkspaceError();

    /// <inheritdoc/>
    public string CreateSnapshot(string name, string description) => throw NoWorkspaceError();

    /// <inheritdoc/>
    public IGenericResult<Solution> RestoreSnapshot(string snapshotId) =>
        GenericResult<Solution>.Failure(WorkspaceResultCodes.ByName("NoSolutionLoaded"));

    /// <inheritdoc/>
    public IEnumerable<SnapshotInfo> ListSnapshots() => [];

    /// <inheritdoc/>
    public bool RemoveSnapshot(string snapshotId) => false;

    /// <inheritdoc/>
    public void ClearSnapshots() { }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> GetChangesFromBaseline() =>
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string>? GetChangesFromSnapshot(string snapshotId) => null;

    /// <inheritdoc/>
    public void ApplyDocumentChanges(IReadOnlyDictionary<string, string> documentChanges) =>
        throw NoWorkspaceError();

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(CancellationToken cancellationToken = default) =>
        throw NoWorkspaceError();

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(
        bool deleteRemovedFiles,
        CancellationToken cancellationToken = default) =>
        throw NoWorkspaceError();

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetAllProjects() => [];

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetLoadedProjects() => [];

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetExcludedProjects() => [];

    /// <inheritdoc/>
    public IReadOnlyList<string> ExcludePatterns => [];

    /// <inheritdoc/>
    public IReadOnlyList<string> LoadDiagnostics => [];

    /// <inheritdoc/>
    public Task<IGenericResult<ProjectInfo>> LoadProject(
        string projectName,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericResult<ProjectInfo>.Failure(WorkspaceResultCodes.ByName("NoSolutionLoaded")));

    /// <inheritdoc/>
    public IGenericResult<ProjectInfo> UnloadProject(string projectName, bool force = false) =>
        GenericResult<ProjectInfo>.Failure(WorkspaceResultCodes.ByName("NoSolutionLoaded"));

    /// <inheritdoc/>
    public bool HasPendingChanges(string projectName) => false;

    /// <inheritdoc/>
    public void SetExcludePatterns(IReadOnlyList<string> patterns) { }
}
