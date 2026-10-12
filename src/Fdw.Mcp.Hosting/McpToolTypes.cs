using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Mcp.Hosting;

/// <summary>
/// TypeCollection of the MCP tool classes a server exposes. Populated at assembly load by the
/// module initializers of every referenced tool package.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(McpToolTypeBase), typeof(IMcpToolType), typeof(McpToolTypes))]
public abstract partial class McpToolTypes : TypeCollectionBase<McpToolTypeBase, IMcpToolType>
{
}
