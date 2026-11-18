using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Mcp;

/// <summary>
/// MessageLogging for serving the WebMCP catalogue over MCP.
/// </summary>
/// <remarks>
/// Codes follow the catalogue: the number carries the same meaning under every prefix, so a tool an
/// agent names that does not exist is 30000 NotFound, a missing argument is 20000
/// RequiredValueMissing, an absent self address source is 60000 ConfigurationMissing and a host that
/// cannot reach its own route is 70000 ConnectionFailed. A host stopped because its source gave no
/// address once it started takes 61000, from the configuration band. Only the operational traces, which no
/// canonical describes, take numbers from this package's open band.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("MCPBRIDGE")]
internal static partial class McpBridgeLog
{
    /// <summary>Logs the catalogue being served to an agent.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolCount">How many tools the registry holds.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug,
        Message = "Serving the WebMCP catalogue over MCP: {toolCount} tool(s)")]
    public static partial IGenericMessage ServingCatalogue(ILogger logger, int toolCount);

    /// <summary>Logs a tool call about to be sent to the host's own route.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolName">The tool the agent named.</param>
    /// <param name="method">The HTTP verb.</param>
    /// <param name="relativeUrl">Path and query the call is sent to.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "MCP tool '{toolName}' calling {method} {relativeUrl}")]
    public static partial IGenericMessage DispatchingToolCall(ILogger logger, string toolName, string method, string relativeUrl);

    /// <summary>Logs the outcome of a tool call.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolName">The tool the agent named.</param>
    /// <param name="statusCode">The status the route answered with.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "MCP tool '{toolName}' answered {statusCode}")]
    public static partial IGenericMessage ToolCallCompleted(ILogger logger, string toolName, int statusCode);

    /// <summary>Logs a failed call being answered with the values its parameter selects from.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolName">The tool the agent named.</param>
    /// <param name="parentRoute">The route listing the valid values.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "MCP tool '{toolName}' failed; listing the values it selects from at {parentRoute}")]
    public static partial IGenericMessage OfferingRecovery(ILogger logger, string toolName, string parentRoute);

    /// <summary>Logs the best-effort recovery listing not being available.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolName">The tool the agent named.</param>
    /// <param name="reason">Why the listing could not be offered.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Why Debug: the tool's own failure is what the agent is told about and is returned intact.
    /// This records only that the extra help could not be attached, which is never the reason a
    /// call failed.
    /// </remarks>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug,
        Message = "MCP tool '{toolName}' could not be offered its valid values: {reason}")]
    public static partial IGenericMessage RecoveryUnavailable(ILogger logger, string toolName, string reason);

    /// <summary>Logs an agent naming a tool this host does not serve.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolName">The name the agent used.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Why Information: an agent naming a tool that is not there is the caller's mistake and is
    /// answered as one. It says nothing about this host's health, and at Warning a single confused
    /// client would look like a fault here.
    /// </remarks>
    [MessageLogging(EventId = 30000, Level = LogLevel.Information,
        Message = "Tool '{toolName}' was not found in this host's WebMCP catalogue")]
    public static partial IGenericMessage ToolNotFound(ILogger logger, string toolName);

    /// <summary>Logs a call that omitted a value the tool's URL cannot be built without.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolName">The tool the agent named.</param>
    /// <param name="argumentName">The argument that was required and absent.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Why Information: the schema already declares the argument required, so this is a caller that
    /// ignored it. Refusing beats sending a URL with a hole in it, which would come back as a 404
    /// the agent could not tell from an empty result.
    /// </remarks>
    [MessageLogging(EventId = 20000, Level = LogLevel.Information,
        Message = "Required value '{argumentName}' was not provided for tool '{toolName}', so its URL cannot be built")]
    public static partial IGenericMessage RequiredArgumentMissing(ILogger logger, string toolName, string argumentName);

    /// <summary>Logs the MCP bridge refused because the host's row never said where its own address comes from.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60000, Level = LogLevel.Error,
        Message = "Required configuration 'SelfAddressSource' is missing from the host's WebMcp row, so a tool call has nowhere to go. Name ListeningAddress (the address the process listens on) or FixedAddress (with SelfBaseAddress). It is never taken from the request's Host header, which the caller controls")]
    public static partial IGenericMessage SelfAddressSourceMissing(ILogger logger);

    /// <summary>Logs the address tool calls go to, once it is known.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="source">The source the row names.</param>
    /// <param name="selfAddress">The address it gave.</param>
    /// <returns>The structured message.</returns>
    // Why Information: once per process, and an operator checking which instance a tool call reaches starts here.
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "MCP bridge self address from source '{source}': tool calls go to {selfAddress}")]
    public static partial IGenericMessage SelfAddressResolved(ILogger logger, string source, string selfAddress);

    /// <summary>Logs a host stopped because its own address could not be worked out once it started.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="source">The source the row names.</param>
    /// <param name="reason">Why the source gave no address.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Why 61000, the configuration band: the row named a source that cannot give an address on this host.
    /// Why Critical: the host is being stopped. A bridge that cannot reach its own routes would accept every
    /// tool call and have nowhere to send it, so it is refused as soon as the address can be read, as a host
    /// with no source is refused at Initialize.
    /// </remarks>
    [MessageLogging(EventId = 61000, Level = LogLevel.Critical,
        Message = "Stopping the host: the MCP bridge's self address source '{source}' gave no address once the host started: {reason}")]
    public static partial IGenericMessage StoppingWithoutSelfAddress(ILogger logger, string source, string reason);

    /// <summary>Logs the MCP server registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug,
        Message = "MCP bridge registered: an MCP server over HTTP serving the WebMCP catalogue")]
    public static partial IGenericMessage Registered(ILogger logger);

    /// <summary>Logs the MCP endpoint mapped.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="path">Where the endpoint is served.</param>
    /// <param name="source">The source the host's own address comes from.</param>
    /// <returns>The structured message.</returns>
    // Why Information: once per process, and an operator connecting an agent starts here.
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "MCP bridge mapped at {path}; tool calls go to the address from source '{source}', worked out once the host has started")]
    public static partial IGenericMessage Mapped(ILogger logger, string path, string source);

    /// <summary>Logs a host that cannot serve the MCP endpoint because it has no route table.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostType">The host's type.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 40000, Level = LogLevel.Error,
        Message = "The MCP bridge maps a route, and this host ({hostType}) has no route table")]
    public static partial IGenericMessage NoRouteTable(ILogger logger, string hostType);

    /// <summary>Logs a host that could not reach its own route.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolName">The tool the agent named.</param>
    /// <param name="target">The address the call was sent to.</param>
    /// <param name="reason">What the transport reported.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Why Error: the browser agent's equivalent call is same-origin and would have arrived. If this
    /// one did not, the configured self address is wrong or the host cannot reach itself, and both
    /// are faults here rather than in the caller.
    /// </remarks>
    [MessageLogging(EventId = 70000, Level = LogLevel.Error,
        Message = "Connection to {target} failed serving MCP tool '{toolName}': {reason}")]
    public static partial IGenericMessage SelfRouteUnreachable(ILogger logger, string toolName, string target, string reason);

    /// <summary>Logs how many of the catalogue's tools a caller was offered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="offered">How many tools the caller may call.</param>
    /// <param name="toolCount">How many tools the registry holds.</param>
    /// <param name="caller">The caller's name, or the placeholder for an unauthenticated caller.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Debug,
        Message = "Offering {offered} of {toolCount} WebMCP tool(s) over MCP to {caller}")]
    public static partial IGenericMessage OfferingTools(ILogger logger, int offered, int toolCount, string caller);

    /// <summary>Logs a tool withheld from a caller because its route would refuse them.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolName">The tool withheld.</param>
    /// <param name="caller">The caller's name, or the placeholder for an unauthenticated caller.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "MCP tool '{toolName}' withheld from {caller}: its route's authorization does not admit them")]
    public static partial IGenericMessage ToolWithheld(ILogger logger, string toolName, string caller);

    /// <summary>Logs a tool withheld from every caller because no route endpoint is known for it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="toolName">The tool withheld.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Why Information: whether a caller may use a tool is decided by its route, and without the route there is
    /// nothing to decide from, so the tool is not offered rather than offered to everyone.
    /// </remarks>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information,
        Message = "MCP tool '{toolName}' withheld: no route endpoint is known for it, so who may call it cannot be judged")]
    public static partial IGenericMessage ToolWithoutEndpoint(ILogger logger, string toolName);

    /// <summary>Logs a tool list asked for outside an HTTP request, so no caller can be judged.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The structured message.</returns>
    /// <remarks>
    /// Why Information: the list follows the caller, and with no request there is no caller, so nothing is offered.
    /// </remarks>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information,
        Message = "The MCP tool list was asked for outside an HTTP request, so no caller can be judged and no tool is offered")]
    public static partial IGenericMessage NoCallerToJudge(ILogger logger);
}
