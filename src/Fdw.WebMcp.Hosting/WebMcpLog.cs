using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// MessageLogging for WebMCP tool discovery and serving operations.
/// EventIds are categorized numbers (<c>Category = Id / 10000</c>). Failures take the catalogue's
/// canonical numbers, so the number carries the meaning: 40000 InvalidState, 60000
/// ConfigurationMissing, 60001 NotConfigured, 60002 NotRegistered, 60004 ConfigurationInvalid.
/// Operational traces take this package's open band: 11018-11035 informational/operational, 51002
/// auth, 61004-61007 configuration.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("HOSTING")]
internal static partial class WebMcpLog
{
    [MessageLogging(EventId = 11018, Level = LogLevel.Debug, Message = "Resolving declared WebMCP tool '{typeName}' against the route table")]
    public static partial IGenericMessage DiscoveringTool(ILogger logger, string typeName);

    [MessageLogging(EventId = 11019, Level = LogLevel.Debug, Message = "WebMCP tool registered: '{name}' → {method} {route}")]
    public static partial IGenericMessage ToolDiscovered(ILogger logger, string name, string route, string method);

    [MessageLogging(EventId = 61004, Level = LogLevel.Warning, Message = "WebMCP tool skipped (no route resolved): '{typeName}'")]
    public static partial IGenericMessage ToolSkipped(ILogger logger, string typeName);

    [MessageLogging(EventId = 61006, Level = LogLevel.Warning, Message = "WebMCP tool skipped (ambiguous route): '{typeName}' maps {candidateCount} route/verb pair(s); set WebMcpTool.HttpMethod to choose one")]
    public static partial IGenericMessage ToolRouteAmbiguous(ILogger logger, string typeName, int candidateCount);

    [MessageLogging(EventId = 11020, Level = LogLevel.Information, Message = "WebMCP registration complete: {count} tool(s) registered")]
    public static partial IGenericMessage ToolsRegistered(ILogger logger, int count);

    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "Serving /.well-known/webmcp.js ({toolCount} tools)")]
    public static partial IGenericMessage ServingWebMcpJs(ILogger logger, int toolCount);

    [MessageLogging(EventId = 51002, Level = LogLevel.Warning, Message = "WebMCP agent key rejected for route '{route}'")]
    public static partial IGenericMessage AgentKeyRejected(ILogger logger, string route);

    [MessageLogging(EventId = 11022, Level = LogLevel.Information, Message = "WebMCP agent key accepted: '{label}' acting as user '{userId}'")]
    public static partial IGenericMessage AgentKeyAccepted(ILogger logger, string label, string userId);

    [MessageLogging(EventId = 11023, Level = LogLevel.Debug, Message = "WebMCP agent request authenticated: '{label}' (userId={userId}) → {route}")]
    public static partial IGenericMessage AgentRequestAuthenticated(ILogger logger, string label, string userId, string route);

    [MessageLogging(EventId = 11025, Level = LogLevel.Trace, Message = "WebMCP route resolved for '{typeName}': '{route}' (via {strategy})")]
    public static partial IGenericMessage RouteResolved(ILogger logger, string typeName, string route, string strategy);

    [MessageLogging(EventId = 11026, Level = LogLevel.Trace, Message = "WebMCP HTTP method resolved for '{typeName}': {httpMethod} (via {strategy})")]
    public static partial IGenericMessage HttpMethodResolved(ILogger logger, string typeName, string httpMethod, string strategy);

    [MessageLogging(EventId = 11027, Level = LogLevel.Trace, Message = "WebMCP endpoint types resolved for '{typeName}': request={requestType}, response={responseType}")]
    public static partial IGenericMessage EndpointTypesResolved(ILogger logger, string typeName, string requestType, string responseType);

    [MessageLogging(EventId = 11028, Level = LogLevel.Trace, Message = "WebMCP generating script for {toolCount} tool(s)")]
    public static partial IGenericMessage GeneratingScript(ILogger logger, int toolCount);

    [MessageLogging(EventId = 11029, Level = LogLevel.Trace, Message = "WebMCP emitted tool '{name}': {httpMethod} {route} ({propertyCount} schema property/properties)")]
    public static partial IGenericMessage ToolScriptEmitted(ILogger logger, string name, string httpMethod, string route, int propertyCount);

    [MessageLogging(EventId = 11030, Level = LogLevel.Trace, Message = "WebMCP mapped schema property '{toolName}.{propertyName}' to type '{jsonType}' format '{format}'")]
    public static partial IGenericMessage SchemaPropertyMapped(ILogger logger, string toolName, string propertyName, string jsonType, string format);

    [MessageLogging(EventId = 11031, Level = LogLevel.Debug, Message = "WebMCP client API key injection for header '{headerName}': {injected}")]
    public static partial IGenericMessage ClientKeyInjection(ILogger logger, string headerName, bool injected);

    [MessageLogging(EventId = 61007, Level = LogLevel.Warning, Message = "WebMCP tool skipped (unbindable route parameter): '{typeName}' route '{route}' needs '{parameterName}' and the request type has no such property, so the URL could never be built")]
    public static partial IGenericMessage ToolParameterUnbindable(ILogger logger, string typeName, string route, string parameterName);

    [MessageLogging(EventId = 11032, Level = LogLevel.Trace, Message = "WebMCP tool '{name}' takes its '{parameterName}' values from '{parentToolName}' ({parentRoute})")]
    public static partial IGenericMessage ParentListResolved(ILogger logger, string name, string parameterName, string parentToolName, string parentRoute);

    [MessageLogging(EventId = 61005, Level = LogLevel.Warning, Message = "WebMCP omitted schema property '{toolName}.{propertyName}' - CLR type '{clrType}' has no JSON Schema mapping, so an agent cannot supply it")]
    public static partial IGenericMessage SchemaPropertySkipped(ILogger logger, string toolName, string propertyName, string clrType);

    [MessageLogging(EventId = 11033, Level = LogLevel.Debug, Message = "WebMCP registered its tool registry, script generator and settings reader")]
    public static partial IGenericMessage Registered(ILogger logger);

    // Why Information: read once, at startup, and it is what this host exposes to agents.
    [MessageLogging(EventId = 11034, Level = LogLevel.Information, Message = "WebMCP settings read from WebMcp row '{configurationName}': header '{headerName}', client key {servesClientKey}, MCP self address {servesMcp}")]
    public static partial IGenericMessage SettingsRead(ILogger logger, string configurationName, string headerName, bool servesClientKey, bool servesMcp);

    // Why Information: once per process, and an operator checking what agents can call starts here.
    [MessageLogging(EventId = 11035, Level = LogLevel.Information, Message = "WebMCP script mapped at {path} with {toolCount} tool(s)")]
    public static partial IGenericMessage ScriptMapped(ILogger logger, string path, int toolCount);

    [MessageLogging(EventId = 40000, Level = LogLevel.Error, Message = "WebMCP maps routes, and this host ({hostType}) has no route table")]
    public static partial IGenericMessage NoRouteTable(ILogger logger, string hostType);

    [MessageLogging(EventId = 60000, Level = LogLevel.Error, Message = "Required configuration '{key}' is missing from the WebMcp row '{configurationName}'")]
    public static partial IGenericMessage ConfigurationMissing(ILogger logger, string configurationName, string key);

    [MessageLogging(EventId = 60001, Level = LogLevel.Error, Message = "WebMCP is not configured: the WebMcp row '{configurationName}' could not be read")]
    public static partial IGenericMessage NotConfigured(ILogger logger, string configurationName);

    [MessageLogging(EventId = 60002, Level = LogLevel.Error, Message = "No {component} registered, so the WebMcp row cannot be read. A host serving WebMCP references the package that supplies the WebMcp domain's implementation (FdwReferenceHosts.WebMcp.Registration for the reference hosts)")]
    public static partial IGenericMessage NotRegistered(ILogger logger, string component);

    [MessageLogging(EventId = 60004, Level = LogLevel.Error, Message = "The WebMcp row '{configurationName}' is invalid: {reason}")]
    public static partial IGenericMessage ConfigurationInvalid(ILogger logger, string configurationName, string reason);
}
