using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Workspace.Management.Logging;

/// <summary>
/// MessageLogging methods for workspace management operations.
/// EventId range: 9016-9050.
/// </summary>
[MessageLoggingTypeCode("MANAGEMENT")]
public static partial class WorkspaceManagementLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // WorkspaceManager Events (9016-9029)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that a workspace is being loaded from a solution path.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "Loading workspace from {solutionPath}")]
    public static partial IGenericMessage WorkspaceLoading(ILogger logger, string solutionPath);

    /// <summary>
    /// Logs that a workspace was loaded successfully.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "Workspace {workspaceId} loaded: {name} with {projectCount} projects")]
    public static partial IGenericMessage WorkspaceLoaded(ILogger logger, Guid workspaceId, string name, int projectCount);

    /// <summary>
    /// Logs that a workspace load failed.
    /// </summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Failed to load workspace from {solutionPath}")]
    public static partial IGenericMessage WorkspaceLoadFailed(ILogger logger, Exception exception, string solutionPath);

    /// <summary>
    /// Logs that a workspace is being unloaded.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Unloading workspace {workspaceId}: {name}")]
    public static partial IGenericMessage WorkspaceUnloading(ILogger logger, Guid workspaceId, string name);

    /// <summary>
    /// Logs that a session was saved with snapshot information.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "Session {sessionId} saved for workspace {workspaceId}: {name} with {snapshotCount} snapshots")]
    public static partial IGenericMessage SessionSaved(ILogger logger, Guid sessionId, Guid workspaceId, string name, int snapshotCount);

    /// <summary>
    /// Logs that a session is being resumed.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Resuming session {sessionId}: {name} from {solutionPath}")]
    public static partial IGenericMessage SessionResuming(ILogger logger, Guid sessionId, string name, string solutionPath);

    /// <summary>
    /// Logs that baseline document changes were applied.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug,
        Message = "Applied {changeCount} baseline document changes")]
    public static partial IGenericMessage BaselineChangesApplied(ILogger logger, int changeCount);

    /// <summary>
    /// Logs that a snapshot was recreated.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug,
        Message = "Recreated snapshot {snapshotName} as {newSnapshotId}")]
    public static partial IGenericMessage SnapshotRecreated(ILogger logger, string snapshotName, string newSnapshotId);

    /// <summary>
    /// Logs that a session was resumed with snapshot count.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "Session {sessionId} resumed as workspace {workspaceId} with {snapshotCount} snapshots")]
    public static partial IGenericMessage SessionResumedWithSnapshots(ILogger logger, Guid sessionId, Guid workspaceId, int snapshotCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // FileBasedSessionStore Events (9030-9039)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that a session was saved to a file.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug,
        Message = "Session {sessionId} saved to {filePath}")]
    public static partial IGenericMessage SessionSavedToFile(ILogger logger, Guid sessionId, string filePath);

    /// <summary>
    /// Logs that a session save failed.
    /// </summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to save session {sessionId}")]
    public static partial IGenericMessage SessionSaveFailed(ILogger logger, Exception exception, Guid sessionId);

    /// <summary>
    /// Logs that a session was loaded from a file.
    /// </summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Debug,
        Message = "Session {sessionId} loaded from {filePath}")]
    public static partial IGenericMessage SessionLoadedFromFile(ILogger logger, Guid sessionId, string filePath);

    /// <summary>
    /// Logs that a session load failed.
    /// </summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "Failed to load session {sessionId}")]
    public static partial IGenericMessage SessionLoadFailed(ILogger logger, Exception exception, Guid sessionId);

    /// <summary>
    /// Logs that a session was deleted.
    /// </summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Debug,
        Message = "Session {sessionId} deleted")]
    public static partial IGenericMessage SessionDeleted(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session delete failed.
    /// </summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "Failed to delete session {sessionId}")]
    public static partial IGenericMessage SessionDeleteFailed(ILogger logger, Exception exception, Guid sessionId);

    /// <summary>
    /// Logs that the session directory was created.
    /// </summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Debug,
        Message = "Created session directory: {directory}")]
    public static partial IGenericMessage SessionDirectoryCreated(ILogger logger, string directory);

    /// <summary>
    /// Logs a warning when loading a session from a file path fails.
    /// </summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning,
        Message = "Failed to load session from {filePath}")]
    public static partial IGenericMessage SessionLoadFromPathFailed(ILogger logger, Exception exception, string filePath);
}
