using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Represents a Roslyn workspace that manages Solution state with snapshot/rollback capabilities.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IWorkspace{T}"/> with Roslyn-specific operations.
/// Solutions in Roslyn are immutable - every modification returns a new Solution instance.
/// </remarks>
public interface IRoslynWorkspace : IWorkspace<Solution>
{
    /// <summary>
    /// Gets the current solution. This is an alias for <see cref="IWorkspace{T}.Current"/>.
    /// </summary>
    Solution CurrentSolution { get; }

    /// <summary>
    /// Gets the baseline solution for change detection. May be null if no baseline has been set.
    /// This is an alias for <see cref="IWorkspace{T}.Baseline"/>.
    /// </summary>
    Solution? BaselineSolution { get; }

    /// <summary>
    /// Updates the current solution. This is an alias for <see cref="IWorkspace{T}.Update"/>.
    /// </summary>
    /// <param name="solution">The new solution to set as current.</param>
    /// <remarks>
    /// Since Roslyn Solutions are immutable, this replaces the current solution reference
    /// with the new solution. Always capture the result of Solution modification methods
    /// and pass them here.
    /// </remarks>
    /// <example>
    /// <code>
    /// // CORRECT - capture and update
    /// var newSolution = workspace.CurrentSolution.AddDocument(...);
    /// workspace.UpdateSolution(newSolution);
    ///
    /// // WRONG - loses changes!
    /// workspace.CurrentSolution.AddDocument(...);
    /// </code>
    /// </example>
    void UpdateSolution(Solution solution);

    /// <summary>
    /// Gets document changes between the baseline and current solution.
    /// </summary>
    /// <returns>
    /// A dictionary of document file paths to their current text content for all
    /// documents that have changed since the baseline was set.
    /// </returns>
    /// <remarks>
    /// Returns an empty dictionary if no baseline is set or no changes exist.
    /// Only includes documents that exist in both solutions but have different content,
    /// or documents that were added since the baseline.
    /// </remarks>
    IReadOnlyDictionary<string, string> GetChangesFromBaseline();

    /// <summary>
    /// Gets document changes between a snapshot and the current solution.
    /// </summary>
    /// <param name="snapshotId">The snapshot to compare against.</param>
    /// <returns>
    /// A dictionary of document file paths to their current text content for all
    /// documents that have changed since the snapshot was created.
    /// Returns null if the snapshot is not found.
    /// </returns>
    IReadOnlyDictionary<string, string>? GetChangesFromSnapshot(string snapshotId);

    /// <summary>
    /// Applies document changes to the current solution.
    /// </summary>
    /// <param name="documentChanges">
    /// A dictionary of document file paths to their new text content.
    /// </param>
    /// <remarks>
    /// For each entry in the dictionary, if the document exists it will be updated;
    /// if not found, it will be skipped (no new documents are created).
    /// </remarks>
    void ApplyDocumentChanges(IReadOnlyDictionary<string, string> documentChanges);

    /// <summary>
    /// Persists any in-memory document changes accumulated since the last apply
    /// (or since solution load if never applied) to disk by writing each changed
    /// document's text to its source <c>FilePath</c>. Resets the apply baseline
    /// to the current solution.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>
    /// Success with the list of file paths that were written. Failure if any
    /// write failed; the failure detail includes per-file error messages.
    /// </returns>
    /// <remarks>
    /// Mutation translators (Rename, ExtractMethod, EncapsulateField, etc.) call
    /// <see cref="UpdateSolution"/> which only swaps the in-memory pointer.
    /// <see cref="ApplyChanges(System.Threading.CancellationToken)"/> is the explicit commit step that writes those
    /// changes to disk — keeping the preview-then-commit workflow Roslyn uses.
    /// </remarks>
    Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes, optionally deleting files whose documents were removed.
    /// </summary>
    /// <param name="deleteRemovedFiles">
    /// When true, files whose documents left the solution are deleted — which is what turns a
    /// cross-project move into a move rather than a copy that leaves a duplicate type behind.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The paths written and deleted.</returns>
    Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(
        bool deleteRemovedFiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all projects in the solution file, including those that are excluded/unloaded.
    /// </summary>
    /// <returns>A list of all project information.</returns>
    IReadOnlyList<ProjectInfo> GetAllProjects();

    /// <summary>
    /// Gets only the projects currently loaded in the workspace.
    /// </summary>
    /// <returns>A list of loaded project information.</returns>
    IReadOnlyList<ProjectInfo> GetLoadedProjects();

    /// <summary>
    /// Gets the projects that are excluded (not loaded) from the workspace.
    /// </summary>
    /// <returns>A list of excluded project information.</returns>
    IReadOnlyList<ProjectInfo> GetExcludedProjects();

    /// <summary>
    /// Gets the current exclude patterns used for project filtering.
    /// </summary>
    IReadOnlyList<string> ExcludePatterns { get; }

    /// <summary>
    /// Loads a project into the workspace.
    /// </summary>
    /// <param name="projectName">The name of the project to load.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Success with the loaded project info, or failure if project not found
    /// or already loaded.
    /// </returns>
    /// <remarks>
    /// If the project has dependencies on other excluded projects, those
    /// dependencies will also be loaded automatically.
    /// </remarks>
    Task<IGenericResult<ProjectInfo>> LoadProject(
        string projectName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a project from the workspace.
    /// </summary>
    /// <param name="projectName">The name of the project to unload.</param>
    /// <param name="force">
    /// If true, unload even if there are pending changes.
    /// If false, fail if the project has pending changes.
    /// </param>
    /// <returns>
    /// Success with the unloaded project info, or failure if project not found,
    /// not loaded, has dependents, or has pending changes.
    /// </returns>
    /// <remarks>
    /// A project cannot be unloaded if other loaded projects depend on it.
    /// Use <see cref="GetAllProjects"/> to check project dependencies.
    /// </remarks>
    IGenericResult<ProjectInfo> UnloadProject(string projectName, bool force = false);

    /// <summary>
    /// Checks if a specific project has pending changes.
    /// </summary>
    /// <param name="projectName">The name of the project to check.</param>
    /// <returns>True if the project has unsaved changes, false otherwise.</returns>
    bool HasPendingChanges(string projectName);

    /// <summary>
    /// Sets the exclude patterns for project filtering.
    /// </summary>
    /// <param name="patterns">The new exclude patterns.</param>
    /// <remarks>
    /// This updates the patterns used for future operations but does not
    /// automatically unload currently loaded projects that match the new patterns.
    /// </remarks>
    void SetExcludePatterns(IReadOnlyList<string> patterns);

    /// <summary>
    /// Gets the problems MSBuild reported while loading this workspace.
    /// </summary>
    /// <remarks>
    /// On the interface so a host can actually report them. These were being captured and then dropped:
    /// a solution that half-loads — projects skipped, a missing SDK, an unresolved reference — produces a
    /// workspace that opens cleanly and then answers every question wrongly, and until this was
    /// reachable the only symptom was findings that made no sense.
    /// </remarks>
    IReadOnlyList<string> LoadDiagnostics { get; }
}
