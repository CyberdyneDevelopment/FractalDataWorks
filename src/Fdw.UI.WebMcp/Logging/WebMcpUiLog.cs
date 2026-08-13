using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.WebMcp.Logging;

/// <summary>
/// MessageLogging for the WebMCP UI layer.
/// </summary>
/// <remarks>
/// The invocation path is deliberately verbose. An agent acting autonomously on a user's session is
/// only safe to the extent that what it attempted, what it was permitted, and what it was refused
/// can be read back afterwards — so attempt, gate decision and outcome each record, correlated by an
/// invocation id, refusals loudest.
/// EventIds are categorized numbers (<c>Category = Id / 10000</c>): 11097-11105 non-error outcomes,
/// 51003 unattributed actions, 91067-91073 errors.
/// </remarks>
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

    /// <summary>
    /// Logs that an agent attempted a tool, with the arguments it supplied. This is the audit
    /// record of the attempt itself and is written before any gate runs, so a refusal further down
    /// still leaves evidence of what was tried.
    /// </summary>
    /// <remarks>
    /// The argument payload is recorded deliberately. An audit line saying only that "a tool was
    /// invoked" cannot answer what the agent tried to do, which is the question asked after the
    /// fact. Callers that accept secrets in tool arguments should redact them before they reach the
    /// bridge - the bridge cannot know which field is sensitive.
    /// </remarks>
    [MessageLogging(
        EventId = 11099,
        Level = LogLevel.Information,
        Message = "WebMCP agent '{agentIdentity}' attempted tool '{toolName}' [invocation {invocationId}] with arguments: {arguments}")]
    public static partial IGenericMessage AgentToolAttempted(
        ILogger logger,
        string agentIdentity,
        string toolName,
        string invocationId,
        string arguments);

    /// <summary> Logs that an agent's tool call completed successfully. </summary>
    [MessageLogging(
        EventId = 11103,
        Level = LogLevel.Information,
        Message = "WebMCP agent '{agentIdentity}' completed tool '{toolName}' [invocation {invocationId}] in {elapsedMilliseconds}ms")]
    public static partial IGenericMessage AgentToolSucceeded(
        ILogger logger,
        string agentIdentity,
        string toolName,
        string invocationId,
        long elapsedMilliseconds);

    /// <summary> Logs that a confirmation-gated tool is waiting on a human decision. </summary>
    [MessageLogging(
        EventId = 11104,
        Level = LogLevel.Information,
        Message = "WebMCP agent '{agentIdentity}' requires confirmation for tool '{toolName}' [invocation {invocationId}]")]
    public static partial IGenericMessage ConfirmationRequested(
        ILogger logger,
        string agentIdentity,
        string toolName,
        string invocationId);

    /// <summary>
    /// Logs that a human approved a confirmation-gated tool. Recorded because an approval is the
    /// point at which responsibility for an autonomous action transfers to a person.
    /// </summary>
    [MessageLogging(
        EventId = 11105,
        Level = LogLevel.Information,
        Message = "WebMCP user approved tool '{toolName}' for agent '{agentIdentity}' [invocation {invocationId}]")]
    public static partial IGenericMessage ConfirmationGranted(
        ILogger logger,
        string agentIdentity,
        string toolName,
        string invocationId);

    /// <summary>
    /// Logs that a tool ran without an attributable agent identity. Warning rather than silence:
    /// an action nobody can be tied to defeats the point of keeping the record.
    /// </summary>
    [MessageLogging(
        EventId = 51003,
        Level = LogLevel.Warning,
        Message = "WebMCP tool '{toolName}' [invocation {invocationId}] ran with no agent identity set on the bridge; the action cannot be attributed")]
    public static partial IGenericMessage UnattributedInvocation(
        ILogger logger,
        string toolName,
        string invocationId);

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
        Message = "WebMCP tool '{toolName}' for agent '{agentIdentity}' [invocation {invocationId}] was not executed: the user declined confirmation")]
    public static partial IGenericMessage ConfirmationDeclined(
        ILogger logger,
        string agentIdentity,
        string toolName,
        string invocationId);

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
        Message = "WebMCP agent '{agentIdentity}' called tool '{toolName}' [invocation {invocationId}], which is not registered on this bridge")]
    public static partial IGenericMessage ToolNotFound(
        ILogger logger,
        string agentIdentity,
        string toolName,
        string invocationId);

    /// <summary> Logs that a tool's execute delegate threw. </summary>
    [MessageLogging(
        EventId = 91070,
        Level = LogLevel.Error,
        Message = "WebMCP tool '{toolName}' for agent '{agentIdentity}' [invocation {invocationId}] failed during execution")]
    public static partial IGenericMessage ToolExecutionFailed(
        ILogger logger,
        Exception ex,
        string agentIdentity,
        string toolName,
        string invocationId);

    /// <summary> Logs that a confirmation-gated tool had no confirmation handler wired. </summary>
    [MessageLogging(
        EventId = 91071,
        Level = LogLevel.Error,
        Message = "WebMCP tool '{toolName}' for agent '{agentIdentity}' [invocation {invocationId}] requires confirmation but the bridge has no ConfirmationHandler; refusing to execute")]
    public static partial IGenericMessage ConfirmationHandlerMissing(
        ILogger logger,
        string agentIdentity,
        string toolName,
        string invocationId);

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
        Message = "WebMCP agent '{agentIdentity}' supplied unparseable arguments for tool '{toolName}' [invocation {invocationId}]: {reason}")]
    public static partial IGenericMessage InvalidArguments(
        ILogger logger,
        string agentIdentity,
        string toolName,
        string invocationId,
        string reason);
}
