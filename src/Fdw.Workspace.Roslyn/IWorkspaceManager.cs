using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Manages multiple Roslyn workspaces with lifecycle management including
/// caching, sleep/wake functionality, and activity tracking.
/// </summary>
/// <remarks>
/// <para>
/// The workspace manager maintains a pool of workspaces that can be opened and closed
/// on demand. Inactive workspaces are put to sleep after a configurable timeout to
/// conserve memory, and are automatically woken when accessed.
/// </para>
/// <para>
/// One workspace can be designated as the "active" workspace which tools will use
/// by default when no specific workspace is specified.
/// </para>
/// </remarks>
public interface IWorkspaceManager : IDisposable
{
    /// <summary>
    /// Gets the currently active workspace, or null if no workspace is active.
    /// </summary>
    IRoslynWorkspace? ActiveWorkspace { get; }

    /// <summary>
    /// Gets the ID of the currently active workspace, or null if no workspace is active.
    /// </summary>
    string? ActiveWorkspaceId { get; }

    /// <summary>
    /// Gets the sleep timeout duration. Workspaces inactive for longer than this
    /// duration will be put to sleep.
    /// </summary>
    TimeSpan SleepTimeout { get; }

    /// <summary>
    /// Opens a solution and creates a managed workspace for it.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln or .slnx file.</param>
    /// <param name="setAsActive">If true, sets this workspace as the active workspace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workspace ID and the opened workspace.</returns>
    Task<(string Id, IRoslynWorkspace Workspace)> OpenSolution(
        string solutionPath,
        bool setAsActive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workspace by its ID, waking it if necessary.
    /// </summary>
    /// <param name="workspaceId">The workspace ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workspace, or null if not found.</returns>
    Task<IRoslynWorkspace?> GetWorkspace(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workspace by its ID synchronously, waking it if necessary.
    /// </summary>
    /// <remarks>
    /// This method safely handles sync-over-async by running the wake operation
    /// on a thread pool thread, avoiding potential deadlocks. Use this when you
    /// need synchronous access to a potentially sleeping workspace.
    /// </remarks>
    /// <param name="workspaceId">The workspace ID.</param>
    /// <returns>A result containing the workspace, or a failure message if not found.</returns>
    IGenericResult<IRoslynWorkspace> GetWorkspaceSync(string workspaceId);

    /// <summary>
    /// Gets a workspace by solution path, waking it if necessary.
    /// </summary>
    /// <param name="solutionPath">Path to the solution file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The workspace, or null if not found.</returns>
    Task<IRoslynWorkspace?> GetWorkspaceByPath(
        string solutionPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active workspace by ID.
    /// </summary>
    /// <param name="workspaceId">The workspace ID to make active.</param>
    /// <returns>True if the workspace was found and set as active.</returns>
    bool SetActiveWorkspace(string workspaceId);

    /// <summary>
    /// Closes a workspace and removes it from management.
    /// </summary>
    /// <param name="workspaceId">The workspace ID to close.</param>
    /// <returns>True if the workspace was found and closed.</returns>
    bool CloseWorkspace(string workspaceId);

    /// <summary>
    /// Closes all managed workspaces.
    /// </summary>
    void CloseAll();

    /// <summary>
    /// Lists all managed workspaces with their current status.
    /// </summary>
    /// <returns>Information about all managed workspaces.</returns>
    IReadOnlyList<ManagedWorkspaceInfo> ListWorkspaces();

    /// <summary>
    /// Manually puts a workspace to sleep to conserve memory.
    /// </summary>
    /// <param name="workspaceId">The workspace ID to put to sleep.</param>
    /// <returns>True if the workspace was found and put to sleep.</returns>
    bool SleepWorkspace(string workspaceId);

    /// <summary>
    /// Manually wakes a sleeping workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace ID to wake.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The awakened workspace, or null if not found.</returns>
    Task<IRoslynWorkspace?> WakeWorkspace(
        string workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks all workspaces and puts inactive ones to sleep.
    /// Called automatically by the manager on a timer.
    /// </summary>
    void CheckAndSleepInactiveWorkspaces();
}
