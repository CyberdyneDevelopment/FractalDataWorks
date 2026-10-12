using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Workspace.Roslyn.Logging;

/// <summary>
/// Static logger class for session-related operations.
/// All log messages use [MessageLogging] for zero string allocation.
/// EventId range: 9210-9219, 9221-9223, 9231-9233, 9240-9250
/// </summary>
/// <remarks>
/// <para>
/// Following FDW Service Infrastructure pattern: every logged message is returned in the result.
/// Event IDs are in the 9xxx range for session operations.
/// </para>
/// </remarks>
[MessageLoggingTypeCode("WS")]
public static partial class SessionLogger
{
    // ========================================================================
    // Session Lifecycle (9210-9219)
    // ========================================================================

    /// <summary>
    /// Logs that a session is being created.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>A message indicating session creation is starting.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Creating session for solution: {solutionPath}")]
    public static partial IGenericMessage CreatingSession(ILogger logger, string solutionPath);

    /// <summary>
    /// Logs that a session was created successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The new session ID.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="description">The session description.</param>
    /// <returns>A message indicating session was created.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Session {sessionId} created for '{solutionPath}': {description}")]
    public static partial IGenericMessage SessionCreated(
        ILogger logger,
        Guid sessionId,
        string solutionPath,
        string description);

    /// <summary>
    /// Logs that a session is being resumed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID being resumed.</param>
    /// <returns>A message indicating session resume is starting.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Resuming session {sessionId}")]
    public static partial IGenericMessage ResumingSession(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session was resumed successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The resumed session ID.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>A message indicating session was resumed.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Session {sessionId} resumed: {solutionPath}")]
    public static partial IGenericMessage SessionResumed(
        ILogger logger,
        Guid sessionId,
        string solutionPath);

    /// <summary>
    /// Logs that a session is being saved.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID being saved.</param>
    /// <returns>A message indicating session save is starting.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Saving session {sessionId}")]
    public static partial IGenericMessage SavingSession(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session was saved successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The saved session ID.</param>
    /// <param name="path">The path where the session was saved.</param>
    /// <returns>A message indicating session was saved.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Information,
        Message = "Session {sessionId} saved to: {path}")]
    public static partial IGenericMessage SessionSaved(
        ILogger logger,
        Guid sessionId,
        string path);

    /// <summary>
    /// Logs that a session is being closed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID being closed.</param>
    /// <returns>A message indicating session close is starting.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Closing session {sessionId}")]
    public static partial IGenericMessage ClosingSession(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session was closed successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The closed session ID.</param>
    /// <returns>A message indicating session was closed.</returns>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Session {sessionId} closed")]
    public static partial IGenericMessage SessionClosed(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session was set as active.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The active session ID.</param>
    /// <returns>A message indicating session is now active.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Active session set to: {sessionId}")]
    public static partial IGenericMessage ActiveSessionSet(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that session metadata was updated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>A message indicating metadata was updated.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Session {sessionId} metadata updated")]
    public static partial IGenericMessage SessionMetadataUpdated(ILogger logger, Guid sessionId);

    // ========================================================================
    // Sleep/Wake (9221-9223)
    // ========================================================================

    /// <summary>
    /// Logs that a session is being put to sleep.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID being put to sleep.</param>
    /// <returns>A message indicating session is sleeping.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Information,
        Message = "Session {sessionId} put to sleep")]
    public static partial IGenericMessage SessionSleeping(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session is being woken.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID being woken.</param>
    /// <returns>A message indicating session is waking.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Waking session {sessionId}")]
    public static partial IGenericMessage WakingSession(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that a session was awakened.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The awakened session ID.</param>
    /// <param name="projectCount">The number of projects in the session.</param>
    /// <returns>A message indicating session was awakened.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Session {sessionId} awakened with {projectCount} projects")]
    public static partial IGenericMessage SessionAwakened(
        ILogger logger,
        Guid sessionId,
        int projectCount);

    // ========================================================================
    // Project Index (9231-9233)
    // ========================================================================

    /// <summary>
    /// Logs that a project session index is being updated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectPath">The project path.</param>
    /// <returns>A message indicating index update is starting.</returns>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Debug,
        Message = "Updating project session index: {projectPath}")]
    public static partial IGenericMessage UpdatingProjectIndex(ILogger logger, string projectPath);

    /// <summary>
    /// Logs that a project session index was updated.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectPath">The project path.</param>
    /// <param name="sessionCount">The number of sessions in the index.</param>
    /// <returns>A message indicating index was updated.</returns>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Project session index updated: {projectPath} ({sessionCount} sessions)")]
    public static partial IGenericMessage ProjectIndexUpdated(
        ILogger logger,
        string projectPath,
        int sessionCount);

    /// <summary>
    /// Logs that a project session index was loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectPath">The project path.</param>
    /// <param name="sessionCount">The number of sessions in the index.</param>
    /// <returns>A message indicating index was loaded.</returns>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Project session index loaded: {projectPath} ({sessionCount} sessions)")]
    public static partial IGenericMessage ProjectIndexLoaded(
        ILogger logger,
        string projectPath,
        int sessionCount);

    // ========================================================================
    // Errors (9240-9250)
    // ========================================================================

    /// <summary>
    /// Logs that no active session exists.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A message indicating no session is active.</returns>
    [MessageLogging(
        EventId = 40000,
        Level = LogLevel.Warning,
        Message = "No active session. Use CreateSession or ResumeSession first.")]
    public static partial IGenericMessage NoActiveSession(ILogger logger);

    /// <summary>
    /// Logs that a session was not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID that was not found.</param>
    /// <returns>A message indicating session was not found.</returns>
    [MessageLogging(
        EventId = 31003,
        Level = LogLevel.Warning,
        Message = "Session {sessionId} not found")]
    public static partial IGenericMessage SessionNotFound(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that session creation failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The solution path.</param>
    /// <param name="reason">The failure reason.</param>
    /// <returns>A message indicating session creation failed.</returns>
    [MessageLogging(
        EventId = 90001,
        Level = LogLevel.Error,
        Message = "Failed to create session for '{solutionPath}': {reason}")]
    public static partial IGenericMessage SessionCreationFailed(
        ILogger logger,
        string solutionPath,
        string reason);

    /// <summary>
    /// Logs that session resume failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="reason">The failure reason.</param>
    /// <returns>A message indicating session resume failed.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Failed to resume session {sessionId}: {reason}")]
    public static partial IGenericMessage SessionResumeFailed(
        ILogger logger,
        Guid sessionId,
        string reason);

    /// <summary>
    /// Logs that session save failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="reason">The failure reason.</param>
    /// <returns>A message indicating session save failed.</returns>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "Failed to save session {sessionId}: {reason}")]
    public static partial IGenericMessage SessionSaveFailed(
        ILogger logger,
        Guid sessionId,
        string reason);

    /// <summary>
    /// Logs that a persisted session was not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>A message indicating persisted session was not found.</returns>
    [MessageLogging(
        EventId = 30000,
        Level = LogLevel.Warning,
        Message = "Persisted session {sessionId} not found in system store")]
    public static partial IGenericMessage PersistedSessionNotFound(ILogger logger, Guid sessionId);

    /// <summary>
    /// Logs that project index update failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="projectPath">The project path.</param>
    /// <param name="reason">The failure reason.</param>
    /// <returns>A message indicating index update failed.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to update project session index '{projectPath}': {reason}")]
    public static partial IGenericMessage ProjectIndexUpdateFailed(
        ILogger logger,
        string projectPath,
        string reason);

    /// <summary>
    /// Logs that session deletion failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="reason">The failure reason.</param>
    /// <returns>A message indicating session deletion failed.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Failed to delete session {sessionId}: {reason}")]
    public static partial IGenericMessage SessionDeleteFailed(
        ILogger logger,
        Guid sessionId,
        string reason);

    /// <summary>
    /// Logs that solution file was not found.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="solutionPath">The solution path.</param>
    /// <returns>A message indicating solution was not found.</returns>
    [MessageLogging(
        EventId = 31005,
        Level = LogLevel.Error,
        Message = "Solution file not found: {solutionPath}")]
    public static partial IGenericMessage SolutionFileNotFound(ILogger logger, string solutionPath);

    /// <summary>
    /// Logs that the session store directory could not be created.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="path">The directory path.</param>
    /// <param name="reason">The failure reason.</param>
    /// <returns>A message indicating directory creation failed.</returns>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "Failed to create session store directory '{path}': {reason}")]
    public static partial IGenericMessage StoreDirectoryCreationFailed(
        ILogger logger,
        string path,
        string reason);

    /// <summary>
    /// Logs that a session load from store failed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="reason">The failure reason.</param>
    /// <returns>A message indicating session load failed.</returns>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Failed to load session {sessionId} from store: {reason}")]
    public static partial IGenericMessage SessionLoadFailed(
        ILogger logger,
        Guid sessionId,
        string reason);
}
