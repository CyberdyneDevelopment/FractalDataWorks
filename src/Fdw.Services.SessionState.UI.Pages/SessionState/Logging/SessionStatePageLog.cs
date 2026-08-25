using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SessionState.UI.Pages.Logging;

/// <summary>
/// MessageLogging for SessionState page operations.
/// EventId range: 4566-4579.
/// </summary>
[MessageLoggingTypeCode("UIPAGES2")]
public static partial class SessionStatePageLog
{
    /// <summary>
    /// Logs that the session state page is loading.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Session state page loading")]
    public static partial IGenericMessage PageLoading(ILogger logger);

    /// <summary>
    /// Logs that session state entries were loaded for the page.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of session state entries loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Session state page loaded {count} entries")]
    public static partial IGenericMessage SessionStateLoaded(ILogger logger, int count);

    /// <summary>
    /// Logs that loading session state for the page failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Session state page failed to load: {error}")]
    public static partial IGenericMessage LoadingFailed(ILogger logger, string error);

    /// <summary>
    /// Logs that the session state list is being rendered.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Rendering session state list")]
    public static partial IGenericMessage RenderingStateList(ILogger logger);

    /// <summary>
    /// Logs that a clear-all operation is being initiated.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "Clearing all session state entries")]
    public static partial IGenericMessage ClearingAll(ILogger logger);

    /// <summary>
    /// Logs that the clear-all operation failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Clear all session state failed: {error}")]
    public static partial IGenericMessage ClearAllFailed(ILogger logger, string error);
}
