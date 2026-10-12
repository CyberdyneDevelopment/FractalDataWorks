using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Mcp.Hosting;

/// <summary>
/// MessageLogging methods for MCP server hosting. EventIds are categorized numbers
/// (<c>Category = Id / 10000</c>), not a contiguous block: 1xxxx informational,
/// 6xxxx configuration/setup.
/// </summary>
[MessageLoggingTypeCode("MCPH")]
public static partial class McpHostingLog
{
    /// <summary>Logs that a tool class was composed onto the server.</summary>
    [MessageLogging(EventId = 11270, Level = LogLevel.Information,
        Message = "MCP tool type registered: '{toolName}' -> {toolClass}")]
    public static partial IGenericMessage ToolTypeRegistered(ILogger logger, string toolName, string toolClass);

    /// <summary>Logs the total number of tool classes composed onto the server.</summary>
    [MessageLogging(EventId = 11271, Level = LogLevel.Information,
        Message = "MCP server composition complete: {count} tool class(es)")]
    public static partial IGenericMessage CompositionComplete(ILogger logger, int count);

    /// <summary>Logs that the server was composed with no tool classes at all.</summary>
    [MessageLogging(EventId = 61200, Level = LogLevel.Critical,
        Message = "No MCP tool packages are referenced - the server would expose zero tools. Reference at least one tool package.")]
    public static partial IGenericMessage NoToolTypesRegistered(ILogger logger);
}
