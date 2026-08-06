using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.WebMcp.Logging;

/// <summary>
/// MessageLogging for the WebMCP UI layer.
/// EventId ranges: 11097-11102 (informational), 91067-91073 (error).
/// </summary>
[MessageLoggingTypeCode("WEBMCP")]
public static partial class WebMcpUiLog
{
    /// <summary> Logs that a bridge published its tools to the browser's model context. </summary>
    [MessageLogging(
        EventId = 11097,
        Level = LogLevel.Information,
        Message = "WebMCP bridge '{handle}' registered {registered} tool(s) for route '{route}'")]
    public static partial IGenericMessage ToolsRegistered(
        ILogger logger,
        string handle,
        int registered,
        string route);

    /// <summary> Logs that the browser exposes no WebMCP model context. </summary>
    [MessageLogging(
        EventId = 11098,
        Level = LogLevel.Debug,
        Message = "WebMCP is unavailable in this browser; {toolCount} tool(s) were not published")]
    public static partial IGenericMessage ModelContextUnavailable(
        ILogger logger,
        int toolCount);

    /// <summary> Logs that an in-browser agent invoked a tool. </summary>
    [MessageLogging(
        EventId = 11099,
        Level = LogLevel.Information,
        Message = "WebMCP tool '{toolName}' invoked by an in-browser agent")]
    public static partial IGenericMessage ToolInvoked(
        ILogger logger,
        string toolName);

    /// <summary> Logs that a bridge tore down its registrations. </summary>
    [MessageLogging(
        EventId = 11100,
        Level = LogLevel.Debug,
        Message = "WebMCP bridge '{handle}' unregistered its tools")]
    public static partial IGenericMessage ToolsUnregistered(
        ILogger logger,
        string handle);

    /// <summary> Logs that a human declined a confirmation-gated tool invocation. </summary>
    [MessageLogging(
        EventId = 11101,
        Level = LogLevel.Information,
        Message = "WebMCP tool '{toolName}' was not executed: the user declined confirmation")]
    public static partial IGenericMessage ConfirmationDeclined(
        ILogger logger,
        string toolName);

    /// <summary> Logs that teardown could not reach the browser (circuit already gone). </summary>
    [MessageLogging(
        EventId = 11102,
        Level = LogLevel.Debug,
        Message = "WebMCP teardown could not reach the browser; the circuit was already disconnected")]
    public static partial IGenericMessage TeardownInterrupted(
        ILogger logger,
        Exception ex);

    /// <summary> Logs that a tool declared an unusable input schema. </summary>
    [MessageLogging(
        EventId = 91067,
        Level = LogLevel.Error,
        Message = "WebMCP tool '{toolName}' declares an InputSchema that is not a JSON object ({reason}); the tool was not registered")]
    public static partial IGenericMessage InvalidInputSchema(
        ILogger logger,
        string toolName,
        string reason);

    /// <summary> Logs that a duplicate tool name was rejected. </summary>
    [MessageLogging(
        EventId = 91068,
        Level = LogLevel.Error,
        Message = "WebMCP tool '{toolName}' is already registered on this bridge; the duplicate registration was rejected")]
    public static partial IGenericMessage DuplicateToolName(
        ILogger logger,
        string toolName);

    /// <summary> Logs that an agent called a tool this bridge does not own. </summary>
    [MessageLogging(
        EventId = 91069,
        Level = LogLevel.Error,
        Message = "WebMCP agent called tool '{toolName}', which is not registered on this bridge")]
    public static partial IGenericMessage ToolNotFound(
        ILogger logger,
        string toolName);

    /// <summary> Logs that a tool's execute delegate threw. </summary>
    [MessageLogging(
        EventId = 91070,
        Level = LogLevel.Error,
        Message = "WebMCP tool '{toolName}' failed during execution")]
    public static partial IGenericMessage ToolExecutionFailed(
        ILogger logger,
        Exception ex,
        string toolName);

    /// <summary> Logs that a confirmation-gated tool had no confirmation handler wired. </summary>
    [MessageLogging(
        EventId = 91071,
        Level = LogLevel.Error,
        Message = "WebMCP tool '{toolName}' requires confirmation but the bridge has no ConfirmationHandler; refusing to execute")]
    public static partial IGenericMessage ConfirmationHandlerMissing(
        ILogger logger,
        string toolName);

    /// <summary> Logs that the browser rejected a tool registration. </summary>
    [MessageLogging(
        EventId = 91072,
        Level = LogLevel.Error,
        Message = "WebMCP browser rejected tool '{toolName}': {reason}")]
    public static partial IGenericMessage RegistrationRejected(
        ILogger logger,
        string toolName,
        string reason);

    /// <summary> Logs that an agent supplied arguments that are not valid JSON. </summary>
    [MessageLogging(
        EventId = 91073,
        Level = LogLevel.Error,
        Message = "WebMCP agent supplied unparseable arguments for tool '{toolName}': {reason}")]
    public static partial IGenericMessage InvalidArguments(
        ILogger logger,
        string toolName,
        string reason);
}
