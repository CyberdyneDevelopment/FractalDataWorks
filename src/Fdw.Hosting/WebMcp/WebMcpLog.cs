using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.WebMcp;

/// <summary>
/// MessageLogging for WebMCP tool discovery and serving operations.
/// EventId range: 7194-7201
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("HOSTING")]
internal static partial class WebMcpLog
{
    [MessageLogging(EventId = 11018, Level = LogLevel.Debug, Message = "Discovering WebMCP tools in assembly type '{typeName}'")]
    public static partial IGenericMessage DiscoveringTool(ILogger logger, string typeName);

    [MessageLogging(EventId = 11019, Level = LogLevel.Debug, Message = "WebMCP tool registered: '{name}' → {method} {route}")]
    public static partial IGenericMessage ToolDiscovered(ILogger logger, string name, string route, string method);

    [MessageLogging(EventId = 61004, Level = LogLevel.Warning, Message = "WebMCP tool skipped (no route resolved): '{typeName}'")]
    public static partial IGenericMessage ToolSkipped(ILogger logger, string typeName);

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
}
