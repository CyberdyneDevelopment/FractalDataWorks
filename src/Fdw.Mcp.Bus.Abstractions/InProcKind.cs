using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Mcp.Bus;

/// <summary>In-process tool source — the MCP tool implementation is a library reference.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(McpToolSourceTypes), "InProc")]
public sealed class InProcKind : McpToolSourceKindBase
{
    /// <summary>Initializes the InProc kind.</summary>
    public InProcKind() : base(1, "InProc") { }
}
