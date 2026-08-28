using System;
using System.Globalization;

namespace Fdw.Mcp.Bus;

/// <summary>
/// Topic-naming conventions for MCP events on the bus. All MCP servers publish to topics under
/// <c>mcp/&lt;server&gt;/&lt;tool&gt;/&lt;phase&gt;</c>. Domain participants (pidgin SQL, pidgin
/// Roslyn) subscribe by glob.
/// </summary>
/// <remarks>
/// Phases:
/// <list type="bullet">
///   <item><description><c>invoke</c> — a tool call has been requested.</description></item>
///   <item><description><c>result</c> — the tool produced a successful result.</description></item>
///   <item><description><c>error</c>  — the tool failed; payload carries the structured failure.</description></item>
/// </list>
/// </remarks>
public static class McpTopics
{
    /// <summary>Phase segment for tool-invocation events.</summary>
    public const string PhaseInvoke = "invoke";

    /// <summary>Phase segment for successful tool-result events.</summary>
    public const string PhaseResult = "result";

    /// <summary>Phase segment for tool-failure events.</summary>
    public const string PhaseError = "error";

    /// <summary>Build a tool-invocation topic, e.g. <c>mcp/mssql/get_table_schema/invoke</c>.</summary>
    public static string ToolInvoke(string serverName, string toolName) =>
        Build(serverName, toolName, PhaseInvoke);

    /// <summary>Build a tool-result topic, e.g. <c>mcp/mssql/get_table_schema/result</c>.</summary>
    public static string ToolResult(string serverName, string toolName) =>
        Build(serverName, toolName, PhaseResult);

    /// <summary>Build a tool-error topic, e.g. <c>mcp/mssql/get_table_schema/error</c>.</summary>
    public static string ToolError(string serverName, string toolName) =>
        Build(serverName, toolName, PhaseError);

    /// <summary>Build a pattern that subscribes to every tool result across servers/tools.</summary>
    public static string AnyToolResult() => "mcp/*/*/result";

    /// <summary>Build a pattern that subscribes to every event from a specific MCP server.</summary>
    public static string AnyServerEvent(string serverName) =>
        string.Format(CultureInfo.InvariantCulture, "mcp/{0}/**", Require(serverName));

    private static string Build(string serverName, string toolName, string phase) =>
        string.Format(CultureInfo.InvariantCulture, "mcp/{0}/{1}/{2}", Require(serverName), Require(toolName), phase);

    private static string Require(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            throw new ArgumentException("Topic segment must not be null or whitespace.", nameof(segment));
        if (segment.IndexOf('/') >= 0)
            throw new ArgumentException("Topic segment may not contain '/'.", nameof(segment));
        return segment;
    }
}
