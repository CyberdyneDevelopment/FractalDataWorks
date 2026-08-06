using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SessionState.Components.Logging;

/// <summary>
/// MessageLogging for SessionStateProvider component operations.
/// EventId range: 4400-4419.
/// </summary>
[MessageLoggingTypeCode("COMPONENTS16")]
public static partial class SessionStateProviderLog
{
    /// <summary>
    /// Logs that session state is being loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Loading session state")]
    public static partial IGenericMessage LoadingState(ILogger logger);

    /// <summary>
    /// Logs that session state was loaded, reporting the number of keys.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of session state keys that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Session state loaded: {count} keys")]
    public static partial IGenericMessage StateLoaded(ILogger logger, int count);

    /// <summary>
    /// Logs that loading session state failed with the given error.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="error">The error message describing the load failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to load session state: {error}")]
    public static partial IGenericMessage LoadStateFailed(ILogger logger, string error);

    /// <summary>
    /// Logs that there is no authenticated user, so session state was not loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 51000,
        Level = LogLevel.Warning,
        Message = "No authenticated user — session state not loaded")]
    public static partial IGenericMessage NoAuthenticatedUser(ILogger logger);

    /// <summary>
    /// Logs that a session state key is being saved.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="key">The session state key being saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Saving session state key '{key}'")]
    public static partial IGenericMessage SavingState(ILogger logger, string key);

    /// <summary>
    /// Logs that a session state key was saved.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="key">The session state key that was saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Session state saved for key '{key}'")]
    public static partial IGenericMessage StateSaved(ILogger logger, string key);

    /// <summary>
    /// Logs that saving session state for a key failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="key">The session state key that failed to save.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to save session state for key '{key}'")]
    public static partial IGenericMessage SaveStateFailed(ILogger logger, string key);

    /// <summary>
    /// Logs that a session state key is being deleted.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="key">The session state key being deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "Deleting session state key '{key}'")]
    public static partial IGenericMessage DeletingState(ILogger logger, string key);

    /// <summary>
    /// Logs that a session state key was deleted.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="key">The session state key that was deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Session state deleted for key '{key}'")]
    public static partial IGenericMessage StateDeleted(ILogger logger, string key);

    /// <summary>
    /// Logs that deleting session state for a key failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="key">The session state key that failed to delete.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Failed to delete session state for key '{key}'")]
    public static partial IGenericMessage DeleteStateFailed(ILogger logger, string key);
}
