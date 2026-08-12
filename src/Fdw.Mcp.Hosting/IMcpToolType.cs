using System;
using Fdw.Collections;

namespace Fdw.Mcp.Hosting;

/// <summary>
/// TypeOption declaring one MCP tool class that a server should expose. A tool package ships one
/// option per tool class; referencing the package is what puts the tool on the server.
/// </summary>
/// <remarks>
/// This is the MCP-server counterpart to how an API server composes its surface: reference an
/// endpoint package and its endpoints appear. Here, reference a tool package and its tool classes
/// appear, because the package's module initializer registers these options at assembly load.
/// </remarks>
public interface IMcpToolType : ITypeOption<int, IMcpToolType>
{
    /// <summary>
    /// The class carrying the ModelContextProtocol <c>[McpServerToolType]</c> attribute whose
    /// <c>[McpServerTool]</c> methods are exposed to agents.
    /// </summary>
    Type ToolClass { get; }
}
