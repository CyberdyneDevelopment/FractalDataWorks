using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Workspace.Roslyn.Logging;

/// <summary>
/// Static logger class for workspace-related operations.
/// All log messages use [MessageLogging] for zero string allocation.
/// EventId range: 9060-9070
/// </summary>
[MessageLoggingTypeCode("WS")]
public static partial class WorkspaceLogger
{
    /// <summary>
    /// Logs a warning that no solution is loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A message indicating no solution is loaded.</returns>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Warning,
        Message = "No solution is loaded. Use the OpenSolution tool to load a solution first.")]
    public static partial IGenericMessage NoSolutionLoaded(ILogger logger);

    /// <summary>
    /// Logs a warning that a workspace was not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="workspaceId">The ID of the workspace that was not found.</param>
    /// <returns>A message indicating the workspace was not found.</returns>
    [MessageLogging(
        EventId = 31006,
        Level = LogLevel.Warning,
        Message = "Workspace {workspaceId} not found")]
    public static partial IGenericMessage WorkspaceNotFound(
        ILogger logger,
        string workspaceId);

    /// <summary>
    /// Logs that a workspace is being woken from sleep.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="workspaceId">The ID of the workspace being woken.</param>
    /// <returns>A message indicating the workspace is waking.</returns>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Information,
        Message = "Waking workspace {workspaceId} from sleep")]
    public static partial IGenericMessage WakingWorkspace(
        ILogger logger,
        string workspaceId);

    /// <summary>
    /// Logs that a workspace has been awakened.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="workspaceId">The ID of the awakened workspace.</param>
    /// <param name="projectCount">The number of projects in the workspace.</param>
    /// <returns>A message indicating the workspace was awakened.</returns>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Information,
        Message = "Workspace {workspaceId} awakened with {projectCount} projects")]
    public static partial IGenericMessage WorkspaceAwakened(
        ILogger logger,
        string workspaceId,
        int projectCount);

    /// <summary>
    /// Logs that a workspace is being put to sleep.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="workspaceId">The ID of the workspace being put to sleep.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>A message indicating the workspace is sleeping.</returns>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Information,
        Message = "Workspace {workspaceId} put to sleep: {solutionPath}")]
    public static partial IGenericMessage WorkspaceSleeping(
        ILogger logger,
        string workspaceId,
        string solutionPath);

    /// <summary>
    /// Logs that a solution is being opened.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>A message indicating the solution is opening.</returns>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Information,
        Message = "Opening solution: {solutionPath}")]
    public static partial IGenericMessage OpeningSolution(
        ILogger logger,
        string solutionPath);

    /// <summary>
    /// Logs that a solution has been opened.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="projectCount">The number of projects in the solution.</param>
    /// <param name="workspaceId">The ID assigned to the workspace.</param>
    /// <returns>A message indicating the solution was opened.</returns>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Information,
        Message = "Solution opened: {solutionPath} with {projectCount} projects (ID: {workspaceId})")]
    public static partial IGenericMessage SolutionOpened(
        ILogger logger,
        string solutionPath,
        int projectCount,
        string workspaceId);

    /// <summary>
    /// Logs that a solution is already open.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>A message indicating the solution is already open.</returns>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Information,
        Message = "Solution already open: {solutionPath}")]
    public static partial IGenericMessage SolutionAlreadyOpen(
        ILogger logger,
        string solutionPath);

    /// <summary>
    /// Logs that the active workspace has been set.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="workspaceId">The ID of the workspace set as active.</param>
    /// <returns>A message indicating the active workspace was set.</returns>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Information,
        Message = "Active workspace set to: {workspaceId}")]
    public static partial IGenericMessage ActiveWorkspaceSet(
        ILogger logger,
        string workspaceId);

    /// <summary>
    /// Logs that a workspace has been closed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="workspaceId">The ID of the closed workspace.</param>
    /// <returns>A message indicating the workspace was closed.</returns>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Information,
        Message = "Workspace closed: {solutionPath} (ID: {workspaceId})")]
    public static partial IGenericMessage WorkspaceClosed(
        ILogger logger,
        string solutionPath,
        string workspaceId);

    /// <summary>
    /// Logs that all workspaces have been closed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A message indicating all workspaces were closed.</returns>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Information,
        Message = "All workspaces closed")]
    public static partial IGenericMessage AllWorkspacesClosed(ILogger logger);
}
