using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>TypeCollection of MCP tool-source kinds. Wave 1 ships InProc + StdioBridge.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(McpToolSourceKindBase), typeof(IMcpToolSourceKind), typeof(McpToolSourceTypes))]
public abstract partial class McpToolSourceTypes : TypeCollectionBase<McpToolSourceKindBase, IMcpToolSourceKind>
{
}
