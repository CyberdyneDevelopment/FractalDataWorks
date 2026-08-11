using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Terminal.Components.Logging;

/// <summary>
/// MessageLogging methods for Terminal component operations.
/// EventId range: 9300-9319
/// </summary>
[MessageLoggingTypeCode("COMPONENTS18")]
public static partial class TerminalComponentLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // HeadlessTerminal — Session Lifecycle (9300-9303)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when a new terminal session is created.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "HeadlessTerminal: Created session '{sessionId}' for user '{userId}'")]
    public static partial IGenericMessage SessionCreated(
        ILogger logger,
        Guid sessionId,
        Guid userId);

    /// <summary>Logs when an existing terminal session is loaded.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "HeadlessTerminal: Loaded existing session '{sessionId}'")]
    public static partial IGenericMessage SessionLoaded(
        ILogger logger,
        Guid sessionId);

    /// <summary>Logs when creating a terminal session fails.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Warning,
        Message = "HeadlessTerminal: Failed to create session for user '{userId}'")]
    public static partial IGenericMessage SessionCreateFailed(
        ILogger logger,
        Guid userId);

    /// <summary>Logs when creating a terminal session fails with an exception.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Warning,
        Message = "HeadlessTerminal: Exception creating session for user '{userId}'")]
    public static partial IGenericMessage SessionCreateException(
        ILogger logger,
        Guid userId,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // HeadlessTerminal — Session Load (9304-9305)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading an existing session fails.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Warning,
        Message = "HeadlessTerminal: Failed to load session '{sessionId}'")]
    public static partial IGenericMessage SessionLoadFailed(
        ILogger logger,
        Guid sessionId);

    /// <summary>Logs when loading an existing session fails with an exception.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Warning,
        Message = "HeadlessTerminal: Exception loading session '{sessionId}'")]
    public static partial IGenericMessage SessionLoadException(
        ILogger logger,
        Guid sessionId,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // HeadlessTerminal — SendCommand (9306-9307)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs a trace entry when a command is sent to the terminal.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "HeadlessTerminal: Sending command to session '{sessionId}'")]
    public static partial IGenericMessage SendingCommand(
        ILogger logger,
        Guid sessionId);

    /// <summary>Logs when sending a command to the terminal fails.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Warning,
        Message = "HeadlessTerminal: Failed to send command to session '{sessionId}'")]
    public static partial IGenericMessage SendCommandFailed(
        ILogger logger,
        Guid sessionId,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // XTermTerminal — JS Interop (9310-9313)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs a trace entry when the xterm.js terminal is initialised.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "XTermTerminal: Initialising xterm.js for session '{sessionId}'")]
    public static partial IGenericMessage XTermInitialising(
        ILogger logger,
        Guid sessionId);

    /// <summary>Logs when xterm.js initialisation fails.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Warning,
        Message = "XTermTerminal: Failed to initialise xterm.js for session '{sessionId}'")]
    public static partial IGenericMessage XTermInitFailed(
        ILogger logger,
        Guid sessionId,
        Exception exception);

    /// <summary>Logs a trace entry when output is written to the xterm.js terminal.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "XTermTerminal: Writing output to session '{sessionId}'")]
    public static partial IGenericMessage XTermWritingOutput(
        ILogger logger,
        Guid sessionId);

    /// <summary>Logs when writing output to xterm.js fails.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Warning,
        Message = "XTermTerminal: Failed to write output to session '{sessionId}'")]
    public static partial IGenericMessage XTermWriteFailed(
        ILogger logger,
        Guid sessionId,
        Exception exception);
}
