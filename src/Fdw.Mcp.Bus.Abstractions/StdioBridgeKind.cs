using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Mcp.Bus;

/// <summary>Stdio-bridge tool source — spawns an external MCP exe and bridges JSON-RPC to the bus.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(McpToolSourceTypes), "StdioBridge")]
public sealed class StdioBridgeKind : McpToolSourceKindBase
{
    /// <summary>Initializes the StdioBridge kind.</summary>
    public StdioBridgeKind() : base(2, "StdioBridge") { }
}
