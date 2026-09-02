using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// MessageLogging for WebMCP tool discovery and serving operations.
/// EventIds are categorized numbers (<c>Category = Id / 10000</c>), drawn from this package's open
/// band: 11018-11031 informational/operational, 51002 auth, 61004-61006 configuration.
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
}
