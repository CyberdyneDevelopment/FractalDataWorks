using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Roslyn;

namespace Fdw.Workspace.Management;

/// <summary>
/// Manages multiple Roslyn workspaces with session persistence and lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// The workspace manager provides:
/// <list type="bullet">
/// <item>Multi-workspace support - load and track multiple solutions simultaneously</item>
/// <item>Session persistence - save and resume workspace state across connections</item>
/// <item>Lifecycle management - proper loading, unloading, and disposal of workspaces</item>
/// </list>
/// </para>
/// <para>
/// Workspaces are identified by GUIDs that remain stable across session save/resume cycles.
/// This enables agents to maintain workspace references across connection interruptions.
/// </para>
/// </remarks>
public interface IWorkspaceManager : IDisposable
{
    /// <summary>
    /// Loads a workspace from a solution file.
    /// </summary>
    /// <param name="solutionPath">The absolute path to the .sln file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A result containing the workspace ID on success, or an error message on failure.
    /// The workspace ID can be used with other methods to access the workspace.
    /// </returns>
    Task<IGenericResult<Guid>> LoadWorkspace(string solutionPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workspace by its ID.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A result containing the workspace on success, or an error if the workspace
    /// is not found or has been unloaded.
    /// </returns>
    Task<IGenericResult<IRoslynWorkspace>> GetWorkspace(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a workspace and releases its resources.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    /// <remarks>
    /// After unloading, the workspace ID is no longer valid unless the session
    /// was saved before unloading.
    /// </remarks>
    Task<IGenericResult<bool>> UnloadWorkspace(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current workspace state to the session store.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A result containing the session ID on success. The session ID can be used
    /// with <see cref="ResumeSession"/> to restore the workspace state.
    /// </returns>
    /// <remarks>
    /// Saving a session captures:
    /// <list type="bullet">
    /// <item>The solution path</item>
    /// <item>Any snapshots created</item>
    /// <item>The baseline reference</item>
    /// <item>Workspace metadata</item>
    /// </list>
    /// </remarks>
    Task<IGenericResult<Guid>> SaveSession(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a previously saved session.
    /// </summary>
    /// <param name="sessionId">The session identifier from a previous <see cref="SaveSession"/> call.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A result containing the workspace ID on success. This may be the same as the
    /// original workspace ID or a new one depending on the session store implementation.
    /// </returns>
    /// <remarks>
    /// Resuming a session:
    /// <list type="bullet">
    /// <item>Reloads the solution from disk</item>
    /// <item>Restores snapshots and baseline state</item>
    /// <item>Returns a valid workspace ID for continued operations</item>
    /// </list>
    /// </remarks>
    Task<IGenericResult<Guid>> ResumeSession(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all currently loaded workspaces.
    /// </summary>
    /// <returns>Information about all active workspaces.</returns>
    IEnumerable<WorkspaceInfo> ListWorkspaces();

    /// <summary>
    /// Lists all saved sessions available for resumption.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about all persisted sessions.</returns>
    Task<IEnumerable<SessionInfo>> ListSessions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a workspace with the given ID is currently loaded.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <returns>True if the workspace is loaded and accessible.</returns>
    bool IsLoaded(Guid workspaceId);

    /// <summary>
    /// Gets the number of currently loaded workspaces.
    /// </summary>
    int WorkspaceCount { get; }
}
