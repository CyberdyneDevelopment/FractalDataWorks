using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Logging methods for Roslyn workspace operations.
/// EventId range: 9001-9015, 9020-9027, 9030-9053
/// </summary>
[MessageLoggingTypeCode("WS")]
public static partial class RoslynWorkspaceLog
{
    /// <summary>
    /// Logs that a solution is being opened.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>A message describing the operation.</returns>
    [MessageLogging(EventId = 11025, Level = LogLevel.Information,
        Message = "Opening solution: {solutionPath}")]
    public static partial IGenericMessage SolutionOpening(ILogger logger, string solutionPath);

    /// <summary>
    /// Logs that a solution was successfully opened.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="projectCount">The number of projects in the solution.</param>
    /// <returns>A message describing the successful operation.</returns>
    [MessageLogging(EventId = 11026, Level = LogLevel.Information,
        Message = "Solution opened: {solutionPath} ({projectCount} projects)")]
    public static partial IGenericMessage SolutionOpened(ILogger logger, string solutionPath, int projectCount);

    /// <summary>
    /// Logs a workspace warning or diagnostic message.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The warning message.</param>
    /// <returns>A message describing the warning.</returns>
    [MessageLogging(EventId = 91001, Level = LogLevel.Warning,
        Message = "Workspace warning: {message}")]
    public static partial IGenericMessage WorkspaceWarning(ILogger logger, string message);

    /// <summary>
    /// Logs that an empty workspace was created.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A message describing the operation.</returns>
    [MessageLogging(EventId = 11027, Level = LogLevel.Debug,
        Message = "Empty workspace created")]
    public static partial IGenericMessage EmptyWorkspaceCreated(ILogger logger);

    /// <summary>
    /// Logs that a snapshot was created.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotId">The unique identifier of the snapshot.</param>
    /// <param name="snapshotName">The name of the snapshot.</param>
    /// <returns>A message describing the snapshot creation.</returns>
    [MessageLogging(EventId = 11028, Level = LogLevel.Information,
        Message = "Snapshot created: {snapshotId} - {snapshotName}")]
    public static partial IGenericMessage SnapshotCreated(ILogger logger, string snapshotId, string snapshotName);

    /// <summary>
    /// Logs that a snapshot was restored.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotId">The unique identifier of the restored snapshot.</param>
    /// <returns>A message describing the snapshot restoration.</returns>
    [MessageLogging(EventId = 11029, Level = LogLevel.Information,
        Message = "Snapshot restored: {snapshotId}")]
    public static partial IGenericMessage SnapshotRestored(ILogger logger, string snapshotId);

    /// <summary>
    /// Logs that a snapshot was not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="snapshotId">The identifier of the snapshot that was not found.</param>
    /// <returns>A message describing the missing snapshot.</returns>
    [MessageLogging(EventId = 31004, Level = LogLevel.Warning,
        Message = "Snapshot not found: {snapshotId}")]
    public static partial IGenericMessage SnapshotNotFound(ILogger logger, string snapshotId);

    /// <summary>
    /// Logs that a project was excluded from loading.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectName">The name of the excluded project.</param>
    /// <returns>A message describing the exclusion.</returns>
    [MessageLogging(EventId = 11030, Level = LogLevel.Debug,
        Message = "Project excluded: {projectName}")]
    public static partial IGenericMessage ProjectExcluded(ILogger logger, string projectName);

    /// <summary>
    /// Logs that a solution was opened with project filtering.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="loadedCount">The number of projects loaded.</param>
    /// <param name="excludedCount">The number of projects excluded.</param>
    /// <returns>A message describing the filtered solution.</returns>
    [MessageLogging(EventId = 11031, Level = LogLevel.Information,
        Message = "Solution opened: {solutionPath} ({loadedCount} loaded, {excludedCount} excluded)")]
    public static partial IGenericMessage SolutionOpenedWithFiltering(
        ILogger logger, string solutionPath, int loadedCount, int excludedCount);

    /// <summary>
    /// Logs that a project was loaded into the workspace.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectName">The name of the loaded project.</param>
    /// <returns>A message describing the load operation.</returns>
    [MessageLogging(EventId = 11032, Level = LogLevel.Information,
        Message = "Project loaded: {projectName}")]
    public static partial IGenericMessage ProjectLoaded(ILogger logger, string projectName);

    /// <summary>
    /// Logs that a project was unloaded from the workspace.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectName">The name of the unloaded project.</param>
    /// <returns>A message describing the unload operation.</returns>
    [MessageLogging(EventId = 11033, Level = LogLevel.Information,
        Message = "Project unloaded: {projectName}")]
    public static partial IGenericMessage ProjectUnloaded(ILogger logger, string projectName);

    /// <summary>
    /// Logs that a project load/unload operation failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectName">The name of the project.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>A message describing the failure.</returns>
    [MessageLogging(EventId = 91002, Level = LogLevel.Warning,
        Message = "Project operation failed for '{projectName}': {reason}")]
    public static partial IGenericMessage ProjectOperationFailed(ILogger logger, string projectName, string reason);

    /// <summary>
    /// Logs that exclude patterns were updated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="patternCount">The number of patterns set.</param>
    /// <returns>A message describing the update.</returns>
    [MessageLogging(EventId = 11034, Level = LogLevel.Information,
        Message = "Exclude patterns updated: {patternCount} patterns")]
    public static partial IGenericMessage ExcludePatternsUpdated(ILogger logger, int patternCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // WorkspaceManager Events (9020-9029)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that a solution is already open.
    /// </summary>
    [MessageLogging(EventId = 11035, Level = LogLevel.Information,
        Message = "Solution already open: {solutionPath}")]
    public static partial IGenericMessage SolutionAlreadyOpen(ILogger logger, string solutionPath);

    /// <summary>
    /// Logs that a solution was opened with project count.
    /// </summary>
    [MessageLogging(EventId = 11036, Level = LogLevel.Information,
        Message = "Solution opened: {solutionPath} with {projectCount} projects (ID: {workspaceId})")]
    public static partial IGenericMessage SolutionOpenedWithId(ILogger logger, string solutionPath, int projectCount, string workspaceId);

    /// <summary>
    /// Logs that an active workspace was set.
    /// </summary>
    [MessageLogging(EventId = 11037, Level = LogLevel.Information,
        Message = "Active workspace set to: {workspaceId}")]
    public static partial IGenericMessage ActiveWorkspaceSet(ILogger logger, string workspaceId);

    /// <summary>
    /// Logs that a workspace was closed.
    /// </summary>
    [MessageLogging(EventId = 11038, Level = LogLevel.Information,
        Message = "Workspace closed: {solutionPath} (ID: {workspaceId})")]
    public static partial IGenericMessage WorkspaceClosed(ILogger logger, string solutionPath, string workspaceId);

    /// <summary>
    /// Logs that all workspaces were closed.
    /// </summary>
    [MessageLogging(EventId = 11039, Level = LogLevel.Information,
        Message = "All workspaces closed")]
    public static partial IGenericMessage AllWorkspacesClosed(ILogger logger);

    /// <summary>
    /// Logs that a workspace was put to sleep.
    /// </summary>
    [MessageLogging(EventId = 11040, Level = LogLevel.Information,
        Message = "Workspace {workspaceId} put to sleep: {solutionPath}")]
    public static partial IGenericMessage WorkspaceSleeping(ILogger logger, string workspaceId, string solutionPath);

    /// <summary>
    /// Logs that eviction was refused because the workspace holds uncommitted work.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="workspaceId">The workspace that stays resident.</param>
    /// <param name="pending">How many documents differ from the baseline.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Information, not Warning: refusing is the correct outcome, and it says plainly why a workspace
    /// the idle policy wanted gone is still using memory. The defect it replaces was silent — evicting
    /// discarded pending edits, and the subsequent commit reported success having written nothing.
    /// </remarks>
    [MessageLogging(EventId = 11059, Level = LogLevel.Information,
        Message = "Workspace {workspaceId} not evicted: {pending} pending document change(s) would be lost")]
    public static partial IGenericMessage WorkspaceSleepRefusedPendingChanges(ILogger logger, string workspaceId, int pending);

    /// <summary>
    /// Logs that eviction was skipped because the workspace was mid-transition.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="workspaceId">The workspace that was busy.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Debug: a workspace being woken is by definition not idle, and the next tick reconsiders it. Worth
    /// recording so a workspace that never gets evicted can be explained rather than guessed at.
    /// </remarks>
    [MessageLogging(EventId = 11060, Level = LogLevel.Debug,
        Message = "Workspace {workspaceId} skipped by the idle check: a wake or sleep is already in progress")]
    public static partial IGenericMessage WorkspaceSleepSkippedBusy(ILogger logger, string workspaceId);

    /// <summary>
    /// Logs that a workspace is being awakened.
    /// </summary>
    [MessageLogging(EventId = 11041, Level = LogLevel.Information,
        Message = "Waking workspace {workspaceId} from sleep")]
    public static partial IGenericMessage WorkspaceWaking(ILogger logger, string workspaceId);

    /// <summary>
    /// Logs that a workspace was awakened with project count.
    /// </summary>
    [MessageLogging(EventId = 11042, Level = LogLevel.Information,
        Message = "Workspace {workspaceId} awakened with {projectCount} projects")]
    public static partial IGenericMessage WorkspaceAwakened(ILogger logger, string workspaceId, int projectCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // RoslynSessionManager Events (9030-9049)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that a session is being created for a solution.
    /// </summary>
    [MessageLogging(EventId = 11043, Level = LogLevel.Information,
        Message = "Creating session for solution: {solutionPath}")]
    public static partial IGenericMessage SessionCreating(ILogger logger, string solutionPath);

    /// <summary>
    /// Logs that a session was created.
    /// </summary>
    [MessageLogging(EventId = 11044, Level = LogLevel.Information,
        Message = "Session {sessionId} created for '{solutionPath}': {description}")]
    public static partial IGenericMessage SessionCreated(ILogger logger, Guid sessionId, string solutionPath, string description);

    /// <summary>
    /// Logs that a session was resumed.
    /// </summary>
    [MessageLogging(EventId = 11045, Level = LogLevel.Information,
        Message = "Session {sessionId} resumed: {solutionPath}")]
    public static partial IGenericMessage SessionResumed(ILogger logger, Guid sessionId, string solutionPath);

    /// <summary>
    /// Logs that a session is being resumed.
    /// </summary>
    [MessageLogging(EventId = 11046, Level = LogLevel.Information,
        Message = "Resuming session {sessionId}")]
    public static partial IGenericMessage SessionResuming(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session is being saved.
    /// </summary>
    [MessageLogging(EventId = 11047, Level = LogLevel.Information,
        Message = "Saving session {sessionId}")]
    public static partial IGenericMessage SessionSaving(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session is being closed.
    /// </summary>
    [MessageLogging(EventId = 11048, Level = LogLevel.Information,
        Message = "Closing session {sessionId}")]
    public static partial IGenericMessage SessionClosing(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session was closed.
    /// </summary>
    [MessageLogging(EventId = 11049, Level = LogLevel.Information,
        Message = "Session {sessionId} closed")]
    public static partial IGenericMessage SessionClosed(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session is being awakened.
    /// </summary>
    [MessageLogging(EventId = 11050, Level = LogLevel.Information,
        Message = "Waking session {sessionId}")]
    public static partial IGenericMessage SessionWaking(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session was awakened with project count.
    /// </summary>
    [MessageLogging(EventId = 11051, Level = LogLevel.Information,
        Message = "Session {sessionId} awakened with {projectCount} projects")]
    public static partial IGenericMessage SessionAwakened(ILogger logger, Guid sessionId, int projectCount);

    /// <summary>
    /// Logs that an active session was set.
    /// </summary>
    [MessageLogging(EventId = 11052, Level = LogLevel.Information,
        Message = "Active session set to: {sessionId}")]
    public static partial IGenericMessage ActiveSessionSet(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that session metadata was updated.
    /// </summary>
    [MessageLogging(EventId = 11053, Level = LogLevel.Information,
        Message = "Session {sessionId} metadata updated")]
    public static partial IGenericMessage SessionMetadataUpdated(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session was put to sleep.
    /// </summary>
    [MessageLogging(EventId = 11054, Level = LogLevel.Information,
        Message = "Session {sessionId} put to sleep")]
    public static partial IGenericMessage SessionSleeping(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a project session index is being updated.
    /// </summary>
    [MessageLogging(EventId = 11055, Level = LogLevel.Debug,
        Message = "Updating project session index: {projectPath}")]
    public static partial IGenericMessage ProjectIndexUpdating(ILogger logger, string projectPath);

    /// <summary>
    /// Logs that project session index update failed.
    /// </summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to update project session index '{projectPath}': {errorMessage}")]
    public static partial IGenericMessage ProjectIndexUpdateFailed(ILogger logger, Exception exception, string projectPath, string errorMessage);

    /// <summary>
    /// Logs that closing a session before delete returned a failure result.
    /// </summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Warning,
        Message = "Close session {sessionId} failed before delete: {errorMessage}")]
    public static partial IGenericMessage SessionCloseFailed(ILogger logger, Guid sessionId, string errorMessage);

    /// <summary>
    /// Logs that saving the project session index returned a failure result.
    /// </summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Warning,
        Message = "Failed to save project session index '{projectPath}': {errorMessage}")]
    public static partial IGenericMessage ProjectIndexSaveFailed(ILogger logger, string projectPath, string errorMessage);

    // ═══════════════════════════════════════════════════════════════════════════
    // FileBasedSessionStore Events (9050-9059)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that a session load failed.
    /// </summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "Failed to load session {sessionId}: {errorMessage}")]
    public static partial IGenericMessage SessionLoadFailed(ILogger logger, Exception exception, Guid sessionId, string errorMessage);

    /// <summary>
    /// Logs that a session was saved to a path.
    /// </summary>
    [MessageLogging(EventId = 11056, Level = LogLevel.Information,
        Message = "Session {sessionId} saved to: {path}")]
    public static partial IGenericMessage SessionSavedToPath(ILogger logger, Guid sessionId, string path);

    /// <summary>
    /// Logs that persisting a session record failed.
    /// </summary>
    /// <remarks>
    /// Warning rather than Error: the in-memory session is still usable, so the caller's operation
    /// succeeded — what is lost is only the ability to reattach to it from a future process.
    /// </remarks>
    [MessageLogging(EventId = 91005, Level = LogLevel.Warning,
        Message = "Session {sessionId} could not be persisted and will not survive this process: {errorMessage}")]
    public static partial IGenericMessage SessionSaveFailed(ILogger logger, Guid sessionId, string errorMessage);

    /// <summary>
    /// Logs that a project session index was loaded.
    /// </summary>
    [MessageLogging(EventId = 11057, Level = LogLevel.Information,
        Message = "Project session index loaded: {projectPath} ({sessionCount} sessions)")]
    public static partial IGenericMessage ProjectIndexLoaded(ILogger logger, string projectPath, int sessionCount);

    /// <summary>
    /// Logs that a project session index was updated.
    /// </summary>
    [MessageLogging(EventId = 11058, Level = LogLevel.Information,
        Message = "Project session index updated: {projectPath} ({sessionCount} sessions)")]
    public static partial IGenericMessage ProjectIndexUpdated(ILogger logger, string projectPath, int sessionCount);

    /// <summary>
    /// Logs that pre-deletion session close failed (best-effort, non-fatal).
    /// </summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Warning,
        Message = "Pre-deletion close of session {sessionId} failed: {error}")]
    public static partial IGenericMessage PreDeletionCloseFailed(ILogger logger, Guid sessionId, string error);

}
